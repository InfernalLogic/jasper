﻿using Baseline;
using Jasper.Persistence.Durability;
using Jasper.Runtime;
using Jasper.Testing.Messaging;
using Shouldly;
using Xunit;

namespace Jasper.Testing.Runtime
{
    public class MessagingRootTester
    {
        [Fact]
        public void create_bus_for_envelope()
        {
            var root = new MockJasperRuntime();
            var original = ObjectMother.Envelope();

            var context1 = new MessageContext(root);
            context1.ReadEnvelope(original, InvocationCallback.Instance);
            var context = (IMessageContext)context1;

            context.Envelope.ShouldBe(original);
            context.Outbox.ShouldNotBeNull();

            context.As<MessageContext>().Outbox.ShouldBeSameAs(context);
        }
    }
}
