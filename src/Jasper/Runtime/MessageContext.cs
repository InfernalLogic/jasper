using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Baseline;
using Jasper.Persistence.Durability;
using Jasper.Transports;
using Jasper.Util;
using Microsoft.Extensions.Logging;

namespace Jasper.Runtime;

public class MessageContext : MessagePublisher, IMessageContext, IEnvelopeOutbox
{
    private IChannelCallback? _channel;
    private object? _sagaId;

    public MessageContext(IJasperRuntime runtime) : base(runtime, Guid.NewGuid().ToString())
    {
    }

    internal IList<Envelope> Scheduled { get; } = new List<Envelope>();

    /// <summary>
    /// Discard all outstanding, cascaded messages and clear the transaction
    /// </summary>
    public async ValueTask ClearAllAsync()
    {
        Scheduled.Clear();
        _outstanding.Clear();

        if (Outbox != null)
        {
            await Outbox.RollbackAsync();
        }

        Outbox = this;
    }

    Task IEnvelopeOutbox.PersistAsync(Envelope envelope)
    {
        _outstanding.Fill(envelope);
        return Task.CompletedTask;
    }

    Task IEnvelopeOutbox.PersistAsync(Envelope[] envelopes)
    {
        _outstanding.Fill(envelopes);
        return Task.CompletedTask;
    }

    Task IEnvelopeOutbox.ScheduleJobAsync(Envelope envelope)
    {
        Scheduled.Fill(envelope);
        return Task.CompletedTask;
    }

    async Task IEnvelopeOutbox.CopyToAsync(IEnvelopeOutbox other)
    {
        await other.PersistAsync(_outstanding.ToArray());

        foreach (var envelope in Scheduled) await other.ScheduleJobAsync(envelope);
    }

    public ValueTask RollbackAsync()
    {
        return ValueTask.CompletedTask;
    }

    internal ValueTask ForwardScheduledEnvelopeAsync(Envelope envelope)
    {
        // TODO -- harden this a bit?
        envelope.Sender = Runtime.GetOrBuildSendingAgent(envelope.Destination);
        envelope.Serializer = Runtime.Options.FindSerializer(envelope.ContentType);

        return persistOrSendAsync(envelope);
    }

    /// <summary>
    ///     Send a response message back to the original sender of the message being handled.
    ///     This can only be used from within a message handler
    /// </summary>
    /// <param name="response"></param>
    /// <param name="context"></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <returns></returns>
    public ValueTask RespondToSenderAsync(object response)
    {
        if (Envelope == null)
        {
            throw new InvalidOperationException(
                "This operation can only be performed while in the middle of handling an incoming message");
        }

        if (Envelope.ReplyUri == null)
        {
            throw new ArgumentOutOfRangeException(nameof(Envelope), $"There is no {nameof(Envelope.ReplyUri)}");
        }

        return SendAsync(Envelope.ReplyUri, response);
    }


    public async Task EnqueueCascadingAsync(object? message)
    {
        if (Envelope == null) throw new InvalidOperationException("No Envelope attached to this context");

        if (Envelope.ResponseType != null && (message?.GetType() == Envelope.ResponseType ||
                                              Envelope.ResponseType.IsAssignableFrom(message?.GetType())))
        {
            Envelope.Response = message;
            return;
        }

        switch (message)
        {
            case null:
                return;

            case ISendMyself sendsMyself:
                await sendsMyself.ApplyAsync(this);
                return;

            case Envelope env:
                throw new InvalidOperationException(
                    "You cannot directly send an Envelope. You may want to use ISendMyself for cascading messages");
                return;

            case IEnumerable<object> enumerable:
                foreach (var o in enumerable) await EnqueueCascadingAsync(o);

                return;
        }

        if (message.GetType().ToMessageTypeName() == Envelope.ReplyRequested)
        {
            await SendAsync(Envelope.ReplyUri!, message);
            return;
        }


        await PublishAsync(message);
    }

    public Envelope? Envelope { get; protected set; }


    public async Task FlushOutgoingMessagesAsync()
    {
        if (!Outstanding.Any()) return;

        foreach (var envelope in Outstanding)
        {
            try
            {
                await envelope.QuickSendAsync();
            }
            catch (Exception e)
            {
                Runtime.Logger.LogError(e,
                    "Unable to send an outgoing message, most likely due to serialization issues");
                Runtime.MessageLogger.DiscardedEnvelope(envelope);
            }
        }

        if (ReferenceEquals(Outbox, this))
        {
            await flushScheduledMessagesAsync();
        }

        _outstanding.Clear();
    }

    public async ValueTask UseInMemoryTransactionAsync()
    {
        if (!ReferenceEquals(this, Outbox))
        {
            await EnlistInOutboxAsync(this);
        }
    }

    public ValueTask CompleteAsync()
    {
        if (_channel == null || Envelope == null)
        {
            throw new InvalidOperationException("No Envelope is active for this context");
        }

        return _channel.CompleteAsync(Envelope);
    }

    public ValueTask DeferAsync()
    {
        if (_channel == null || Envelope == null)
        {
            throw new InvalidOperationException("No Envelope is active for this context");
        }

        return _channel.DeferAsync(Envelope);
    }

    public async Task ReScheduleAsync(DateTimeOffset scheduledTime)
    {
        if (_channel == null || Envelope == null)
        {
            throw new InvalidOperationException("No Envelope is active for this context");
        }

        Envelope.ScheduledTime = scheduledTime;
        if (_channel is ISupportNativeScheduling c)
        {
            await c.MoveToScheduledUntilAsync(Envelope, Envelope.ScheduledTime.Value);
        }
        else
        {
            await Persistence.ScheduleJobAsync(Envelope);
        }
    }

    public async Task MoveToDeadLetterQueueAsync(Exception exception)
    {
        if (_channel == null || Envelope == null)
        {
            throw new InvalidOperationException("No Envelope is active for this context");
        }

        if (_channel is ISupportDeadLetterQueue c)
        {
            await c.MoveToErrorsAsync(Envelope, exception);
        }
        else
        {
            // If persistable, persist
            await Persistence.MoveToDeadLetterStorageAsync(Envelope, exception);
        }
    }

    public Task RetryExecutionNowAsync()
    {
        if (_channel == null || Envelope == null)
        {
            throw new InvalidOperationException("No Envelope is active for this context");
        }

        return Runtime.Pipeline.InvokeAsync(Envelope, _channel!);
    }

    internal void ClearState()
    {
        _outstanding.Clear();
        Scheduled.Clear();
        Envelope = null;
        Outbox = null;
        _sagaId = null;
    }

    internal void ReadEnvelope(Envelope? originalEnvelope, IChannelCallback channel)
    {
        Envelope = originalEnvelope ?? throw new ArgumentNullException(nameof(originalEnvelope));
        CorrelationId = originalEnvelope.CorrelationId;
        _channel = channel;
        _sagaId = originalEnvelope.SagaId;

        Outbox = this;

        if (Envelope.AckRequested && Envelope.ReplyUri != null)
        {
            var ack = new Acknowledgement { CorrelationId = Envelope.Id };
            var ackEnvelope = Runtime.RoutingFor(typeof(Acknowledgement)).RouteToDestination(ack, Envelope.ReplyUri, null);
            _outstanding.Add(ackEnvelope);
        }
    }

    private async Task flushScheduledMessagesAsync()
    {
        if (Persistence is NullEnvelopePersistence)
        {
            foreach (var envelope in Scheduled) Runtime.ScheduleLocalExecutionInMemory(envelope.ScheduledTime!.Value, envelope);
        }
        else
        {
            foreach (var envelope in Scheduled) await Persistence.ScheduleJobAsync(envelope);
        }

        Scheduled.Clear();
    }

    protected override void trackEnvelopeCorrelation(Envelope outbound)
    {
        base.trackEnvelopeCorrelation(outbound);
        outbound.SagaId = _sagaId?.ToString() ?? Envelope?.SagaId ?? outbound.SagaId;

        if (Envelope != null)
        {
            outbound.CausationId = Envelope.Id.ToString();
        }
    }

    public async ValueTask SendAcknowledgementAsync()
    {
        if (Envelope!.ReplyUri == null) return;

        var acknowledgement = new Acknowledgement
        {
            CorrelationId = Envelope.Id
        };

        var envelope = Runtime.RoutingFor(typeof(Acknowledgement))
            .RouteToDestination(acknowledgement, Envelope.ReplyUri, null);

        trackEnvelopeCorrelation(envelope);
        envelope.SagaId = Envelope.SagaId;
        // TODO -- reevaluate the metadata. Causation, ORiginator, all that

        try
        {
            await envelope.StoreAndForwardAsync();
        }
        catch (Exception e)
        {
            // TODO -- any kind of retry? Only an issue for inline senders anyway
            Runtime.Logger.LogError(e, "Failure while sending an acknowledgement for envelope {Id}", envelope.Id);
        }
    }

    public async ValueTask SendFailureAcknowledgementAsync(string failureDescription)
    {
        if (Envelope!.ReplyUri == null) return;

        var acknowledgement = new FailureAcknowledgement
        {
            CorrelationId = Envelope.Id,
            Message = failureDescription
        };

        var envelope = Runtime.RoutingFor(typeof(FailureAcknowledgement))
            .RouteToDestination(acknowledgement, Envelope.ReplyUri, null);

        trackEnvelopeCorrelation(envelope);
        envelope.SagaId = Envelope.SagaId;
        // TODO -- reevaluate the metadata. Causation, ORiginator, all that

        try
        {
            await envelope.StoreAndForwardAsync();
        }
        catch (Exception e)
        {
            // TODO -- any kind of retry? Only an issue for inline senders anyway
            Runtime.Logger.LogError(e, "Failure while sending a failure acknowledgement for envelope {Id}", envelope.Id);
        }
    }
}
