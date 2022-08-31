﻿using System;
using System.Threading.Tasks;
using Marten;

namespace Jasper.Persistence.Marten;

public static class MessageContextExtensions
{
    /// <summary>
    ///     Enlists the current IMessageContext in the Marten session's transaction
    ///     lifecycle
    /// </summary>
    /// <param name="context"></param>
    /// <param name="session"></param>
    /// <returns></returns>
    public static Task EnlistInOutboxAsync(this IMessageContext context, IDocumentSession session)
    {
        if (context.Transaction is MartenEnvelopeOutbox)
        {
            throw new InvalidOperationException(
                "This execution context is already enrolled in a Marten Envelope Outbox");
        }

        var persistence = new MartenEnvelopeOutbox(session, context);
        session.Listeners.Add(new FlushOutgoingMessagesOnCommit(context));

        return context.EnlistInOutboxAsync(persistence);
    }
}
