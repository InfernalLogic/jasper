﻿using System;
using System.Threading.Tasks;
using Jasper.ErrorHandling;
using Microsoft.Extensions.Logging;

namespace Jasper.Runtime;

public class MessageSucceededContinuation : IContinuation
{
    public static readonly MessageSucceededContinuation Instance = new();

    private MessageSucceededContinuation()
    {
    }

    public async ValueTask ExecuteAsync(IMessageContext context,
        IJasperRuntime runtime,
        DateTimeOffset now)
    {
        try
        {
            await context.FlushOutgoingMessagesAsync();

            await context.CompleteAsync();

            runtime.MessageLogger.MessageSucceeded(context.Envelope!);
        }
        catch (Exception ex)
        {
            await context.SendFailureAcknowledgementAsync("Sending cascading message failed: " + ex.Message);

            runtime.Logger.LogError(ex, "Failure while post-processing a successful envelope");
            runtime.MessageLogger.MessageFailed(context.Envelope!, ex);

            await new MoveToErrorQueue(ex).ExecuteAsync(context, runtime, now);
        }
    }
}
