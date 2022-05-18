using System;
using System.Collections.Generic;

namespace Jasper.RabbitMQ.Internal
{
    public class RabbitMqTransportExpression : IRabbitMqTransportExpression
    {
        private readonly RabbitMqTransport _transport;
        private readonly JasperOptions _options;

        public RabbitMqTransportExpression(RabbitMqTransport transport, JasperOptions options)
        {
            _transport = transport;
            _options = options;
        }

        public IBindingExpression BindExchange(string exchangeName, Action<RabbitMqExchange>? configure = null)
        {
            DeclareExchange(exchangeName, configure);
            return new BindingExpression(exchangeName, this);
        }

        internal class BindingExpression : IBindingExpression
        {
            private readonly string _exchangeName;
            private readonly RabbitMqTransportExpression _parent;

            internal BindingExpression(string exchangeName, RabbitMqTransportExpression parent)
            {
                _exchangeName = exchangeName;
                _parent = parent;
            }

            public IRabbitMqTransportExpression ToQueue(string queueName, Action<RabbitMqQueue>? configure = null,
                Dictionary<string, object>? arguments = null)
            {
                _parent.DeclareQueue(queueName, configure);
                ToQueue(queueName, $"{_exchangeName}_{queueName}");

                return _parent;
            }

            public IRabbitMqTransportExpression ToQueue(string queueName, string bindingKey,
                Action<RabbitMqQueue>? configure = null, Dictionary<string, object>? arguments = null)
            {
                _parent.DeclareQueue(queueName, configure);

                var binding = _parent._transport.Exchanges[_exchangeName].BindQueue(queueName, bindingKey);

                if (arguments != null)
                {
                    foreach (var argument in arguments)
                    {
                        binding.Arguments[argument.Key] = argument.Value;
                    }
                }

                return _parent;
            }
        }



        public IRabbitMqTransportExpression UseConventionalRouting(Action<RabbitMqMessageRoutingConvention>? configure = null)
        {
            var convention = new RabbitMqMessageRoutingConvention();
            configure?.Invoke(convention);
            _options.RoutingConventions.Add(convention);

            return this;
        }

        IRabbitMqTransportExpression IRabbitMqTransportExpression.AutoProvision()
        {
            _transport.AutoProvision = true;
            return this;
        }

        IRabbitMqTransportExpression IRabbitMqTransportExpression.AutoPurgeOnStartup()
        {
            _transport.AutoPurgeAllQueues = true;
            return this;
        }


        public IRabbitMqTransportExpression DeclareExchange(string exchangeName,
            Action<RabbitMqExchange>? configure = null)
        {
            var exchange = _transport.Exchanges[exchangeName];
            configure?.Invoke(exchange);

            return this;
        }

        public IBindingExpression BindExchange(string exchangeName, ExchangeType exchangeType)
        {
            return BindExchange(exchangeName, e => e.ExchangeType = exchangeType);
        }

        public IRabbitMqTransportExpression DeclareQueue(string queueName, Action<RabbitMqQueue>? configure = null)
        {
            var queue = _transport.Queues[queueName];
            configure?.Invoke(queue);

            return this;
        }

        public IRabbitMqTransportExpression DeclareExchange(string exchangeName, ExchangeType exchangeType,
            bool isDurable = true, bool autoDelete = false)
        {
            return DeclareExchange(exchangeName, e =>
            {
                e.ExchangeType = exchangeType;
                e.IsDurable = isDurable;
                e.AutoDelete = autoDelete;
            });
        }
    }
}
