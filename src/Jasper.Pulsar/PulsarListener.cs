using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;
using DotPulsar.Abstractions;
using DotPulsar.Extensions;
using Jasper.Transports;

namespace Jasper.Pulsar
{
    internal class PulsarListener : IListener, IAsyncDisposable
    {
        private IConsumer<ReadOnlySequence<byte>>? _consumer;
        private readonly Task? _receivingLoop;
        private readonly CancellationToken _cancellation;
        private readonly PulsarSender _sender;
        private IReceiver _receiver;
        private readonly CancellationTokenSource _localCancellation;

        public PulsarListener(PulsarEndpoint endpoint, IReceiver receiver, PulsarTransport transport,
            CancellationToken cancellation)
        {
            var endpoint1 = endpoint;
            _cancellation = cancellation;

            Address = endpoint.Uri;

            _sender = new PulsarSender(endpoint, transport, _cancellation);

            _receiver = receiver;

            _localCancellation = new CancellationTokenSource();

            var combined = CancellationTokenSource.CreateLinkedTokenSource(_cancellation, _localCancellation.Token);

            _receiver = receiver;

            _consumer = transport.Client!.NewConsumer()
                .SubscriptionName("Jasper")
                // TODO -- more options here. Give the user complete
                // control over the Pulsar usage. Maybe expose ConsumerOptions on endpoint
                .Topic(endpoint1.PulsarTopic())
                .Create();

            _receivingLoop = Task.Run(async () =>
            {
                await foreach (var message in _consumer.Messages(cancellationToken: combined.Token))
                {
                    var envelope = new PulsarEnvelope(message);

                    // TODO -- invoke the deserialization here. A
                    envelope.Data = message.Data.ToArray();
                    endpoint1.MapIncomingToEnvelope(envelope, message);

                    // TODO -- the worker queue should already have the Uri,
                    // so just take in envelope
                    await receiver!.ReceivedAsync(this, envelope);
                }
            }, combined.Token);
        }

        public ValueTask CompleteAsync(Envelope envelope)
        {
            if (envelope is PulsarEnvelope e)
            {
                if (_consumer != null)
                {
                    return _consumer.Acknowledge(e.MessageData, _cancellation);
                }
            }

            return ValueTask.CompletedTask;
        }

        public async ValueTask DeferAsync(Envelope envelope)
        {
            if (envelope is PulsarEnvelope e)
            {
                await _consumer!.Acknowledge(e.MessageData, _cancellation);
                await _sender.SendAsync(envelope);
            }
        }

        public async ValueTask DisposeAsync()
        {
            _localCancellation.Cancel();

            if (_consumer != null)
            {
                await _consumer.DisposeAsync();
            }

            await _sender.DisposeAsync();

            _receivingLoop!.Dispose();
        }

        public Uri Address { get; }
        public async ValueTask StopAsync()
        {
            if (_consumer == null) return;
            await _consumer.Unsubscribe(_cancellation);
            await _consumer.RedeliverUnacknowledgedMessages(_cancellation);
        }

        public async Task<bool> TryRequeueAsync(Envelope envelope)
        {
            if (envelope is PulsarEnvelope)
            {
                await _sender.SendAsync(envelope);
                return true;
            }

            return false;
        }
    }
}
