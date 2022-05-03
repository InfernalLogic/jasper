using System;
using System.Threading;
using System.Threading.Tasks;
using Baseline;
using Jasper.Persistence.Durability;
using Jasper.Runtime.Scheduled;
using Jasper.Runtime.WorkerQueues;
using Jasper.Transports;
using Jasper.Transports.Local;
using Lamar;
using Microsoft.Extensions.Logging;

namespace Jasper.Runtime;

public partial class JasperRuntime
{
    private void startInMemoryScheduledJobs()
    {
        ScheduledJobs =
            new InMemoryScheduledJobProcessor((IWorkerQueue)AgentForLocalQueue(TransportConstants.Replies));

        // Bit of a hack, but it's necessary. Came up in compliance tests
        if (Persistence is NulloEnvelopePersistence p)
        {
            p.ScheduledJobs = ScheduledJobs;
        }
    }

    private async Task startMessagingTransports()
    {
        foreach (var transport in Options)
        {
            await transport.InitializeAsync(this).ConfigureAwait(false);
            foreach (var endpoint in transport.Endpoints())
            {
                endpoint.Root = this; // necessary to locate serialization
            }
        }

        foreach (var transport in Options)
        {
            transport.StartSenders(this);
        }

        foreach (var transport in Options)
        {
            transport.StartListeners(this);
        }

        foreach (var subscriber in Options.Subscribers)
        {
            _subscribers.Fill(subscriber);
        }
    }

    private async Task startDurabilityAgent()
    {
        // HOKEY, BUT IT WORKS
        if (_container.Model.DefaultTypeFor<IEnvelopePersistence>() != typeof(NulloEnvelopePersistence) &&
            Options.Advanced.DurabilityAgentEnabled)
        {
            var durabilityLogger = _container.GetInstance<ILogger<DurabilityAgent>>();

            // TODO -- use the worker queue for Retries?
            var worker = new DurableWorkerQueue(new LocalQueueSettings("scheduled"), Pipeline, Advanced, Persistence,
                Logger);

            Durability = new DurabilityAgent(this, Logger, durabilityLogger, worker, Persistence,
                Options.Advanced);

            await Durability.StartAsync(Options.Advanced.Cancellation);
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            Task ret;
            // Build up the message handlers
            await Handlers.CompileAsync(Options, _container);

            await startMessagingTransports();

            startInMemoryScheduledJobs();

            _durableLocalQueue = GetOrBuildSendingAgent(TransportConstants.DurableLocalUri);

            await startDurabilityAgent();
        }
        catch (Exception? e)
        {
            MessageLogger.LogException(e, message: "Failed to start the Jasper messaging");
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_hasStopped) return;

        _hasStopped = true;

        // This is important!
        _container.As<Container>().DisposalLock = DisposalLock.Unlocked;


        if (Durability != null) await Durability.StopAsync(cancellationToken);

        Advanced.Cancel();
    }
}