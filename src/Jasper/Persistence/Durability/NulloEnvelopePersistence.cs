using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Jasper.Logging;
using Jasper.Runtime.Scheduled;

namespace Jasper.Persistence.Durability
{
    public class NulloEnvelopePersistence : IEnvelopePersistence, IEnvelopeStorageAdmin
    {
        public IEnvelopeStorageAdmin Admin => this;
        public IDurabilityAgentStorage AgentStorage { get; } = null;
        public IScheduledJobProcessor ScheduledJobs { get; set; }

        public Task DeleteIncomingEnvelopes(Envelope[] envelopes)
        {
            return Task.CompletedTask;
        }

        public Task DeleteIncomingEnvelope(Envelope envelope)
        {
            return Task.CompletedTask;
        }

        public Task DeleteOutgoing(Envelope[] envelopes)
        {
            return Task.CompletedTask;
        }

        public Task DeleteOutgoing(Envelope envelope)
        {
            return Task.CompletedTask;
        }

        public Task MoveToDeadLetterStorage(ErrorReport[] errors)
        {
            return Task.CompletedTask;
        }

        public Task MoveToDeadLetterStorage(Envelope envelope, Exception ex)
        {
            return Task.CompletedTask;
        }

        public Task ScheduleExecution(Envelope[] envelopes)
        {
            return Task.CompletedTask;
        }

        public Task<ErrorReport> LoadDeadLetterEnvelope(Guid id)
        {
            return Task.FromResult<ErrorReport>(null);
        }

        public Task<Envelope[]> AllIncomingEnvelopes()
        {
            return Task.FromResult(new Envelope[0]);
        }

        public Task<Envelope[]> AllOutgoingEnvelopes()
        {
            return Task.FromResult(new Envelope[0]);
        }

        public Task ReleaseAllOwnership()
        {
            return Task.CompletedTask;
        }

        public Task IncrementIncomingEnvelopeAttempts(Envelope envelope)
        {
            return Task.CompletedTask;
        }

        public Task StoreIncoming(Envelope envelope)
        {
            if (envelope.Status == EnvelopeStatus.Scheduled)
            {
                if (envelope.ExecutionTime == null) throw new ArgumentOutOfRangeException($"The envelope {envelope} is marked as Scheduled, but does not have an ExecutionTime");
                ScheduledJobs?.Enqueue(envelope.ExecutionTime.Value, envelope);
            }
            return Task.CompletedTask;
        }

        public Task StoreIncoming(Envelope[] envelopes)
        {
            foreach (var envelope in envelopes.Where(x => x.Status == EnvelopeStatus.Scheduled))
            {
                ScheduledJobs?.Enqueue(envelope.ExecutionTime.Value, envelope);
            }

            return Task.CompletedTask;
        }

        public Task DiscardAndReassignOutgoing(Envelope[] discards, Envelope[] reassigned, int nodeId)
        {
            return Task.CompletedTask;
        }

        public Task StoreOutgoing(Envelope envelope, int ownerId)
        {
            return Task.CompletedTask;
        }

        public Task StoreOutgoing(Envelope[] envelopes, int ownerId)
        {
            return Task.CompletedTask;
        }

        public Task<PersistedCounts> GetPersistedCounts()
        {
            // Nothing to do, but keeps the metrics from blowing up
            return Task.FromResult(new PersistedCounts());
        }

        public void Describe(TextWriter writer)
        {
            writer.WriteLine("No persistent envelope storage");
        }

        public Task ClearAllPersistedEnvelopes()
        {
            Console.WriteLine("There is no durable envelope storage");
            return Task.CompletedTask;
        }

        public Task RebuildSchemaObjects()
        {
            Console.WriteLine("There is no durable envelope storage");
            return Task.CompletedTask;
        }

        public string CreateSql()
        {
            Console.WriteLine("There is no durable envelope storage");
            return string.Empty;
        }

        public Task ScheduleJob(Envelope envelope)
        {
            ScheduledJobs?.Enqueue(envelope.ExecutionTime.Value, envelope);

            return Task.CompletedTask;
        }


    }
}
