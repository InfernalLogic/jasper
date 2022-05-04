using System;
using System.Collections.Generic;
using Jasper.Transports;
using RabbitMQ.Client;

namespace Jasper.RabbitMQ.Internal
{
    public class RabbitMqExchange
    {
        private readonly RabbitMqTransport _parent;
        public string Name { get; }

        public RabbitMqExchange(string name, RabbitMqTransport parent)
        {
            _parent = parent;
            Name = name;
            DeclaredName = name == TransportConstants.Default ? "" : Name;
        }

        public bool IsDurable { get; set; } = true;

        public string DeclaredName { get; }

        public ExchangeType ExchangeType { get; set; } = ExchangeType.Fanout;


        public bool AutoDelete { get; set; } = false;

        public IDictionary<string, object> Arguments { get; } = new Dictionary<string, object>();

        internal void Declare(IModel channel)
        {
            if (DeclaredName == string.Empty) return;
            var exchangeTypeName = ExchangeType.ToString().ToLower();
            channel.ExchangeDeclare(DeclaredName, exchangeTypeName, IsDurable, AutoDelete, Arguments);
        }


        public void Teardown(IModel channel)
        {
            if (DeclaredName == string.Empty) return;
            channel.ExchangeDelete(DeclaredName);
        }
    }
}
