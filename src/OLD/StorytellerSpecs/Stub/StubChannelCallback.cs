﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Jasper;
using Jasper.Transports;

namespace StorytellerSpecs.Stub
{
    public class StubChannelCallback : IChannelCallback
    {
        private readonly StubEndpoint _endpoint;
        private readonly Envelope _envelope;
        public readonly IList<Envelope> Sent = new List<Envelope>();

        public StubChannelCallback(StubEndpoint endpoint, Envelope envelope)
        {
            _endpoint = endpoint;
            _envelope = envelope;
        }

        public bool MarkedSucessful { get; set; }

        public Exception Exception { get; set; }

        public bool MarkedFailed { get; set; }

        public DateTime? DelayedTo { get; set; }

        public bool WasMovedToErrors { get; set; }

        public bool Requeued { get; set; }

        public Task Complete(Envelope envelope)
        {
            MarkedSucessful = true;
            return Task.CompletedTask;
        }

        public Task MoveToErrors(Exception exception)
        {
            WasMovedToErrors = true;
            return Task.CompletedTask;
        }

        public Task Defer(Envelope envelope)
        {
            Requeued = true;


            return _endpoint.EnqueueOutgoing(_envelope);
        }

        public Task MoveToScheduledUntil(DateTimeOffset time)
        {
            throw new NotImplementedException();
        }
    }
}
