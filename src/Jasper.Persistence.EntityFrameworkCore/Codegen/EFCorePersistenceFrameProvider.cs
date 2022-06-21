using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Baseline;
using Baseline.ImTools;
using Jasper.Configuration;
using Jasper.Persistence.Sagas;
using Lamar;
using LamarCodeGeneration;
using LamarCodeGeneration.Frames;
using LamarCodeGeneration.Model;
using Microsoft.EntityFrameworkCore;
using TypeExtensions = Baseline.TypeExtensions;

namespace Jasper.Persistence.EntityFrameworkCore.Codegen;

// ReSharper disable once InconsistentNaming
public class EFCorePersistenceFrameProvider : ISagaPersistenceFrameProvider, ITransactionFrameProvider
{
    public void ApplyTransactionSupport(IChain chain, IContainer container)
    {
        var dbType = DetermineDbContextType(chain, container);

        chain.Middleware.Insert(0, new EnrollDbContextInTransaction(dbType));


        var saveChangesAsync =
            dbType.GetMethod(nameof(DbContext.SaveChangesAsync), new[] { typeof(CancellationToken) });

        var call = new MethodCall(dbType, saveChangesAsync)
        {
            CommentText = "Added by EF Core Transaction Middleware"
        };

        chain.Postprocessors.Add(call);

        if (chain.ShouldFlushOutgoingMessages())
        {
            chain.Postprocessors.Add(MethodCall.For<IExecutionContext>(x => x.FlushOutgoingMessagesAsync()));
        }
    }

    private ImHashMap<Type, Type> _dbContextTypes = ImHashMap<Type, Type>.Empty;

    internal Type DetermineDbContextType(Type entityType, IContainer container)
    {
        if (_dbContextTypes.TryFind(entityType, out var dbContextType))
        {
            return dbContextType;
        }

        using var nested = container.GetNestedContainer();
        var candidates = container.Model.ServiceTypes.Where(x => x.ServiceType.CanBeCastTo<DbContext>())
            .Select(x => x.ServiceType).ToArray();

        foreach (var candidate in candidates)
        {
            var dbContext = (DbContext)nested.GetInstance(candidate);
            if (dbContext.Model.FindEntityType(entityType) != null)
            {
                _dbContextTypes = _dbContextTypes.AddOrUpdate(entityType, candidate);
                return candidate;
            }
        }

        throw new ArgumentOutOfRangeException(nameof(entityType),
            $"Cannot find a DbContext type that has a mapping for {entityType.FullNameInCode()}");
    }

    public Type DetermineSagaIdType(Type sagaType, IContainer container)
    {
        var dbContextType = DetermineDbContextType(sagaType, container);
        using var nested = container.GetNestedContainer();
        var context = (DbContext)nested.GetInstance(dbContextType);
        var config = context.Model.FindEntityType(sagaType);
        if (config == null)
            throw new InvalidOperationException(
                $"Could not find entity configuration for {sagaType.FullNameInCode()} in DbContext {context}");

        return config.FindPrimaryKey()?.GetKeyType() ??
               throw new InvalidOperationException(
                   $"No known primary key for {sagaType.FullNameInCode()} in DbContext {context}");
    }

    public Frame DetermineLoadFrame(IContainer container, Type sagaType, Variable sagaId)
    {
        var dbContextType = DetermineDbContextType(sagaType, container);
        return new LoadEntityFrame(dbContextType, sagaType, sagaId);
    }

    public Frame DetermineInsertFrame(Variable saga, IContainer container)
    {
        var dbContextType = DetermineDbContextType(saga.VariableType, container);
        return new DbContextOperationFrame(dbContextType, saga, nameof(DbContext.Add));
    }

    public Frame CommitUnitOfWorkFrame(Variable saga, IContainer container)
    {
        var dbContextType = DetermineDbContextType(saga.VariableType, container);
        var method =
            dbContextType.GetMethod(nameof(DbContext.SaveChangesAsync), new Type[] { typeof(CancellationToken) });

        return new MethodCall(dbContextType, method);
    }

    public Frame DetermineUpdateFrame(Variable saga, IContainer container)
    {
        return new CommentFrame("No explicit update necessary with EF Core");
    }

    public Frame DetermineDeleteFrame(Variable sagaId, Variable saga, IContainer container)
    {
        var dbContextType = DetermineDbContextType(saga.VariableType, container);
        return new DbContextOperationFrame(dbContextType, saga, nameof(DbContext.Remove));
    }

    public static Type DetermineDbContextType(IChain chain, IContainer container)
    {
        var contextTypes = chain.ServiceDependencies(container).Where(x => TypeExtensions.CanBeCastTo<DbContext>(x)).ToArray();

        if (contextTypes.Length == 0)
        {
            throw new InvalidOperationException(
                $"Cannot determine the {nameof(DbContext)} type for {chain.Description}");
        }

        if (contextTypes.Length > 1)
        {
            throw new InvalidOperationException(
                $"Cannot determine the {nameof(DbContext)} type for {chain.Description}, multiple {nameof(DbContext)} types detected: {contextTypes.Select(x => x.Name).Join(", ")}");
        }

        return contextTypes.Single();
    }

    public class EnrollDbContextInTransaction : SyncFrame
    {
        private readonly Type _dbContextType;
        private Variable? _context;
        private Variable? _dbContext;

        public EnrollDbContextInTransaction(Type dbContextType)
        {
            _dbContextType = dbContextType;
        }

        public override void GenerateCode(GeneratedMethod method, ISourceWriter writer)
        {
            writer.WriteComment("Enroll the DbContext & IMessagingContext in the outgoing Jasper outbox transaction");
            writer.Write(
                $"await {typeof(JasperEnvelopeEntityFrameworkCoreExtensions).FullName}.{nameof(JasperEnvelopeEntityFrameworkCoreExtensions.EnlistInOutboxAsync)}({_context!.Usage}, {_dbContext!.Usage});");

            Next?.GenerateCode(method, writer);
        }

        public override IEnumerable<Variable> FindVariables(IMethodVariables chain)
        {
            _context = chain.FindVariable(typeof(IExecutionContext));
            yield return _context;

            _dbContext = chain.FindVariable(_dbContextType);
            yield return _dbContext;
        }
    }
}

internal class DbContextOperationFrame : SyncFrame
{
    private readonly Type _dbContextType;
    private readonly Variable _saga;
    private readonly string _methodName;
    private Variable _context;

    public DbContextOperationFrame(Type dbContextType, Variable saga, string methodName)
    {
        _dbContextType = dbContextType;
        _saga = saga;
        _methodName = methodName;
    }

    public override IEnumerable<Variable> FindVariables(IMethodVariables chain)
    {
        _context = chain.FindVariable(_dbContextType);
        yield return _context;
    }

    public override void GenerateCode(GeneratedMethod method, ISourceWriter writer)
    {
        writer.Write($"{_context.Usage}.{_methodName}({_saga.Usage});");
        Next?.GenerateCode(method, writer);
    }
}

internal class LoadEntityFrame : AsyncFrame
{
    private readonly Type _dbContextType;
    private readonly Type _sagaType;
    private readonly Variable _sagaId;
    private Variable _context;
    private Variable _cancellation;

    public LoadEntityFrame(Type dbContextType, Type sagaType, Variable sagaId)
    {
        _dbContextType = dbContextType;
        _sagaType = sagaType;
        _sagaId = sagaId;

        Saga = new Variable(sagaType, this);
    }

    public Variable Saga { get; }

    public override IEnumerable<Variable> FindVariables(IMethodVariables chain)
    {
        _context = chain.FindVariable(_dbContextType);
        yield return _context;

        _cancellation = chain.FindVariable(typeof(CancellationToken));
        yield return _cancellation;
    }

    public override void GenerateCode(GeneratedMethod method, ISourceWriter writer)
    {
        writer.Write($"var {Saga.Usage} = await {_context.Usage}.{nameof(DbContext.FindAsync)}<{Saga.VariableType.FullNameInCode()}>({_sagaId.Usage}).ConfigureAwait(false);");
        Next?.GenerateCode(method, writer);
    }


}
