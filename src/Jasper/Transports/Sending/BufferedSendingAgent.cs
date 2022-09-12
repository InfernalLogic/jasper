using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Jasper.Configuration;
using Jasper.Logging;
using Jasper.Runtime;
using Microsoft.Extensions.Logging;

namespace Jasper.Transports.Sending;

internal class BufferedSendingAgent : SendingAgent
{
    private List<Envelope> _queued = new();

    public BufferedSendingAgent(ILogger logger, IMessageLogger messageLogger, ISender sender,
        AdvancedSettings settings, Endpoint endpoint) : base(logger, messageLogger, sender, settings, endpoint)
    {
    }

    public override bool IsDurable => false;

    public override Task EnqueueForRetryAsync(OutgoingMessageBatch batch)
    {
        _queued.AddRange(batch.Messages);
        _queued.RemoveAll(e => e.IsExpired());

        if (_queued.Count > Endpoint.MaximumEnvelopeRetryStorage)
        {
            var toRemove = _queued.Count - Endpoint.MaximumEnvelopeRetryStorage;
            _queued = _queued.Skip(toRemove).ToList();
        }

        return Task.CompletedTask;
    }

    protected override async Task afterRestartingAsync(ISender sender)
    {
        var toRetry = _queued.Where(x => !x.IsExpired()).ToArray();
        _queued.Clear();

        foreach (var envelope in toRetry) await _senderDelegate(envelope);
    }

    public override Task MarkSuccessfulAsync(OutgoingMessageBatch outgoing)
    {
        return MarkSuccessAsync();
    }

    public override Task MarkSuccessfulAsync(Envelope outgoing)
    {
        return MarkSuccessAsync();
    }

    protected override async Task storeAndForwardAsync(Envelope envelope)
    {
        using var activity = JasperTracing.StartSending(envelope);
        try
        {
            await _senderDelegate(envelope);
        }
        finally
        {
            activity?.Stop();
        }

    }
}
