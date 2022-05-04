using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Baseline;
using DotPulsar;
using DotPulsar.Abstractions;
using Jasper.Runtime;
using Jasper.Transports;

namespace Jasper.Pulsar
{
    public class PulsarTransport : TransportBase<PulsarEndpoint>, IAsyncDisposable
    {
        public const string ProtocolName = "pulsar";

        private readonly LightweightCache<Uri, PulsarEndpoint> _endpoints;

        public PulsarTransport() : base(ProtocolName, "Pulsar")
        {
            Builder = PulsarClient.Builder();

            _endpoints =
                new LightweightCache<Uri, PulsarEndpoint>(uri => new PulsarEndpoint(uri, this));
        }

        public PulsarEndpoint this[Uri uri] => _endpoints[uri];

        public IPulsarClientBuilder Builder { get; }

        protected override IEnumerable<PulsarEndpoint> endpoints()
        {
            return _endpoints;
        }

        protected override PulsarEndpoint findEndpointByUri(Uri uri)
        {
            return _endpoints[uri];
        }

        public override ValueTask InitializeAsync(IJasperRuntime root)
        {
            Client = Builder.Build();
            return ValueTask.CompletedTask;
        }

        internal IPulsarClient? Client { get; private set; }

        public ValueTask DisposeAsync()
        {
            base.Dispose();

            if (Client != null) return Client.DisposeAsync();

            return ValueTask.CompletedTask;
        }

        public PulsarEndpoint EndpointFor(string topicPath)
        {
            var uri = PulsarEndpoint.UriFor(topicPath);
            return this[uri];
        }
    }
}
