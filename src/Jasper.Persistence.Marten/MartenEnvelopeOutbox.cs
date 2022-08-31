﻿using System;
using System.Threading.Tasks;
using Jasper.Persistence.Durability;
using Jasper.Persistence.Marten.Persistence.Operations;
using Jasper.Persistence.Postgresql;
using Marten;

namespace Jasper.Persistence.Marten;

public class MartenEnvelopeOutbox : IEnvelopeOutbox
{
    private readonly int _nodeId;
    private readonly IDocumentSession _session;
    private readonly PostgresqlSettings _settings;

    public MartenEnvelopeOutbox(IDocumentSession session, IMessageContext bus)
    {
        if (bus.Persistence is PostgresqlEnvelopePersistence persistence)
        {
            _settings = (PostgresqlSettings)persistence.DatabaseSettings;
            _nodeId = persistence.Settings.UniqueNodeId;
        }
        else
        {
            throw new InvalidOperationException(
                "This Jasper application is not using Postgresql + Marten as the backing message persistence");
        }

        _session = session;
    }

    public Task PersistAsync(Envelope envelope)
    {
        _session.StoreOutgoing(_settings, envelope, _nodeId);
        return Task.CompletedTask;
    }

    public Task PersistAsync(Envelope[] envelopes)
    {
        foreach (var envelope in envelopes) _session.StoreOutgoing(_settings, envelope, _nodeId);

        return Task.CompletedTask;
    }

    public Task ScheduleJobAsync(Envelope envelope)
    {
        _session.StoreIncoming(_settings, envelope);
        return Task.CompletedTask;
    }

    public Task CopyToAsync(IEnvelopeOutbox other)
    {
        throw new NotSupportedException();
    }
}
