using Shouldly;
using Xunit;

namespace Jasper.RabbitMQ.Tests
{
    public class auto_declaration_of_rabbit_resources
    {
        [Fact]
        public void declare_a_queue_from_listener()
        {
            var options = new JasperOptions();
            options.ListenToRabbitQueue("queue1");

            var transport = options.RabbitMqTransport();

            transport.Queues.Has("queue1").ShouldBeTrue();

            // Defaults
            var queue = transport.Queues["queue1"];
            queue.AutoDelete.ShouldBeFalse();
            queue.IsDurable.ShouldBeTrue();
            queue.IsExclusive.ShouldBeFalse();
        }

        [Fact]
        public void declare_an_exchange_when_mapping_a_rabbit_exchange_to_senders()
        {
            var options = new JasperOptions();
            options.PublishAllMessages().ToRabbitExchange("exchange1");

            var transport = options.RabbitMqTransport();
            transport.Exchanges.Has("exchange1").ShouldBeTrue();

            var exchange = transport.Exchanges["exchange1"];
            exchange.AutoDelete.ShouldBeFalse();
            exchange.Name.ShouldBe("exchange1");
            exchange.ExchangeType.ShouldBe(ExchangeType.Fanout);
            exchange.IsDurable.ShouldBeTrue();
        }
    }
}
