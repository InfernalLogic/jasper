﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Baseline.Dates;
using IntegrationTests;
using Jasper.Attributes;
using Jasper.Configuration;
using Jasper.Persistence.Durability;
using Jasper.Persistence.SqlServer;
using Jasper.Persistence.SqlServer.Persistence;
using Jasper.Transports;
using Jasper.Util;
using Microsoft.Extensions.Hosting;
using Oakton.Resources;
using Shouldly;
using TestingSupport;
using Xunit;

namespace Jasper.Persistence.Testing.SqlServer.Persistence
{
    [MessageIdentity("Message1")]
    public class Message1
    {
        public Guid Id = Guid.NewGuid();
    }


    public class SqlServerBackedMessagePersistenceTests : SqlServerContext, IAsyncLifetime
    {
        public SqlServerBackedMessagePersistenceTests()
        {

        }

        protected override async Task initialize()
        {
            theHost = JasperHost.For(opts =>
            {
                opts.PersistMessagesWithSqlServer(Servers.SqlServerConnectionString);
            });

            await theHost.ResetResourceState();

            theEnvelope = ObjectMother.Envelope();
            theEnvelope.Message = new Message1();
            theEnvelope.ScheduledTime = DateTime.Today.ToUniversalTime().AddDays(1);
            theEnvelope.CorrelationId = Guid.NewGuid().ToString();
            theEnvelope.ConversationId = Guid.NewGuid();
            theEnvelope.ParentId = Guid.NewGuid().ToString();

            theHost.Get<IEnvelopePersistence>().ScheduleJobAsync(theEnvelope).Wait(3.Seconds());

            var persistor = theHost.Get<SqlServerEnvelopePersistence>();

            persisted = (await persistor.Admin.AllIncomingAsync())
                .FirstOrDefault(x => x.Id == theEnvelope.Id);
        }

        public override Task DisposeAsync()
        {
            return theHost.StopAsync();
        }

        private IHost theHost;
        private Envelope theEnvelope;
        private Envelope persisted;

        [Fact]
        public void should_be_in_scheduled_status()
        {
            persisted.Status.ShouldBe(EnvelopeStatus.Scheduled);
        }

        [Fact]
        public void should_bring_across_correlation_information()
        {
            persisted.CorrelationId.ShouldBe(theEnvelope.CorrelationId);
            persisted.ParentId.ShouldBe(theEnvelope.ParentId);
            persisted.ConversationId.ShouldBe(theEnvelope.ConversationId);
        }

        [Fact]
        public void should_be_owned_by_any_node()
        {
            persisted.OwnerId.ShouldBe(TransportConstants.AnyNode);
        }

        [Fact]
        public void should_persist_the_scheduled_envelope()
        {
            persisted.ShouldNotBeNull();
        }
    }
}
