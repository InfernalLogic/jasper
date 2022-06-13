using System;
using System.Collections.Generic;
using System.Reflection;
using LamarCodeGeneration;
using LamarCodeGeneration.Frames;
using LamarCodeGeneration.Model;
using Marten.Events;

namespace Jasper.Persistence.Marten.Codegen;

public class MissingAggregateCheckFrame : SyncFrame
{
    private readonly Type _aggregateType;
    private readonly Type _commandType;
    private readonly MemberInfo _aggregateIdMember;
    private readonly Variable _eventStream;
    private Variable? _command;

    public MissingAggregateCheckFrame(Type aggregateType, Type commandType, MemberInfo aggregateIdMember, Variable eventStream)
    {
        _aggregateType = aggregateType;
        _commandType = commandType;
        _aggregateIdMember = aggregateIdMember;
        _eventStream = eventStream;
    }

    public override IEnumerable<Variable> FindVariables(IMethodVariables chain)
    {
        _command = chain.FindVariable(_commandType);
        yield return _command;
    }

    public override void GenerateCode(GeneratedMethod method, ISourceWriter writer)
    {
        writer.WriteLine($"if ({_eventStream.Usage}.{nameof(IEventStream<string>.Aggregate)} == null) throw new {typeof(UnknownAggregateException).FullNameInCode()}(typeof({_aggregateType.FullNameInCode()}), {_command!.Usage}.{_aggregateIdMember.Name});");

        Next?.GenerateCode(method, writer);
    }
}
