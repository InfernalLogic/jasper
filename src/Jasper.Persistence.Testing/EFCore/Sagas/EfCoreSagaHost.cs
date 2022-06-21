﻿using System;
using System.Threading.Tasks;
using IntegrationTests;
using Jasper.Persistence.Database;
using Jasper.Persistence.EntityFrameworkCore;
using Jasper.Persistence.SqlServer;
using Jasper.Persistence.Testing.SqlServer;
using Jasper.Runtime.Handlers;
using Jasper.Tracking;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Oakton.Resources;
using TestingSupport;
using TestingSupport.Sagas;
using Weasel.Core;
using Weasel.SqlServer;

namespace Jasper.Persistence.Testing.EFCore.Sagas
{
    public class EfCoreSagaHost : ISagaHost
    {
        private IHost _host;

        public IHost BuildHost<TSaga>()
        {
            _host = JasperHost.For(opts =>
            {
                opts.Handlers.DisableConventionalDiscovery().IncludeType<TSaga>();

                opts.PersistMessagesWithSqlServer(Servers.SqlServerConnectionString);

                opts.Services.AddDbContext<SagaDbContext>(x => x.UseSqlServer(Servers.SqlServerConnectionString));

                opts.UseEntityFrameworkCorePersistence();

                opts.PublishAllMessages().Locally();
            });

            // Watch if this hangs, might have to get fancier
            Initialize().GetAwaiter().GetResult();

            return _host;
        }

        public Task<T> LoadState<T>(Guid id) where T : class
        {
            var session = _host.Get<SagaDbContext>();
            return session.FindAsync<T>(id).AsTask();
        }

        public Task<T> LoadState<T>(int id) where T : class
        {
            var session = _host.Get<SagaDbContext>();
            return session.FindAsync<T>(id).AsTask();
        }

        public Task<T> LoadState<T>(long id) where T : class
        {
            var session = _host.Get<SagaDbContext>();
            return session.FindAsync<T>(id).AsTask();
        }

        public Task<T> LoadState<T>(string id) where T : class
        {
            var session = _host.Get<SagaDbContext>();
            return session.FindAsync<T>(id).AsTask();
        }

        public async Task Initialize()
        {
            var tables = new ISchemaObject[]
            {
                new WorkflowStateTable<Guid>("GuidWorkflowState"),
                new WorkflowStateTable<int>("IntWorkflowState"),
                new WorkflowStateTable<long>("LongWorkflowState"),
                new WorkflowStateTable<string>("StringWorkflowState"),
            };

            await using var conn = new SqlConnection(Servers.SqlServerConnectionString);
            await conn.OpenAsync();

            var migration = await SchemaMigration.Determine(conn, tables);
            await new SqlServerMigrator().ApplyAll(conn, migration, AutoCreate.All);

            await _host.ResetResourceState();
        }

    }
}
