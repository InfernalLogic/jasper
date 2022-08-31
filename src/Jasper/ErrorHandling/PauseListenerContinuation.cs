using System;
using System.Threading;
using System.Threading.Tasks;
using Jasper.Runtime;

namespace Jasper.ErrorHandling;

public class PauseListenerContinuation : IContinuation, IContinuationSource
{
    public PauseListenerContinuation(TimeSpan pauseTime)
    {
        PauseTime = pauseTime;
    }

    public TimeSpan PauseTime { get; }

    public ValueTask ExecuteAsync(IMessageContext context, IJasperRuntime runtime, DateTimeOffset now)
    {
        var agent = runtime.FindListeningAgent(context.Envelope!.Listener!.Address);


        if (agent != null)
        {
#pragma warning disable VSTHRD110
            Task.Factory.StartNew(async () =>
#pragma warning restore VSTHRD110
            {
                await agent.PauseAsync(PauseTime);
            },CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
        }

        return ValueTask.CompletedTask;
    }

    public string Description => "Pause all message processing on this listener for " + PauseTime;
    public IContinuation Build(Exception ex, Envelope envelope)
    {
        return this;
    }
}
