using System.Linq;
using System.Threading.Tasks;
using Jasper.Configuration;
using Jasper.RabbitMQ.Internal;
using Jasper.Runtime;
using Jasper.Runtime.Routing;
using Jasper.Util;
using Shouldly;
using Xunit;

namespace Jasper.RabbitMQ.Tests.ConventionalRouting
{
    public class when_discovering_a_sender_with_all_defaults : ConventionalRoutingContext
    {
        private readonly MessageRoute theRoute;

        public when_discovering_a_sender_with_all_defaults()
        {
            theRoute = PublishingRoutesFor<PublishedMessage>().Single();
        }

        [Fact]
        public void should_have_exactly_one_route()
        {
            theRoute.ShouldNotBeNull();
        }

        [Fact]
        public void routed_to_rabbit_mq_exchange()
        {
            var endpoint = theRoute.Sender.Endpoint.ShouldBeOfType<RabbitMqEndpoint>();
            endpoint.ExchangeName.ShouldBe(typeof(PublishedMessage).ToMessageTypeName());
        }

        [Fact]
        public void endpoint_mode_is_buffered_by_default()
        {
            var endpoint = theRoute.Sender.Endpoint.ShouldBeOfType<RabbitMqEndpoint>();
            endpoint.Mode.ShouldBe(EndpointMode.BufferedInMemory);
        }

        [Fact]
        public async Task has_declared_exchange()
        {
            // The rabbit object construction is lazy, so force it to happen
            await new MessagePublisher(theRuntime).SendAsync(new PublishedMessage());

            var endpoint = theRoute.Sender.Endpoint.ShouldBeOfType<RabbitMqEndpoint>();
            theTransport.Exchanges.Has(endpoint.ExchangeName).ShouldBeTrue();
            var theExchange = theTransport.Exchanges[endpoint.ExchangeName];
            theExchange.HasDeclared.ShouldBeTrue();
        }

        [Fact]
        public async Task has_bound_the_exchange_to_a_queue_of_the_same_name()
        {
            // The rabbit object construction is lazy, so force it to happen
            await new MessagePublisher(theRuntime).SendAsync(new PublishedMessage());

            var endpoint = theRoute.Sender.Endpoint.ShouldBeOfType<RabbitMqEndpoint>();
            var theExchange = theTransport.Exchanges[endpoint.ExchangeName];
            var binding = theExchange.Bindings().Single().ShouldNotBeNull();
            binding.Queue.Name.ShouldBe(theExchange.Name);
            binding.Queue.HasDeclared.ShouldBeTrue();
            binding.HasDeclared.ShouldBeTrue();
        }


    }
}
