﻿using System;
using System.Threading.Tasks;
using Jasper.Logging;
using Jasper.Persistence.Durability;
using Jasper.Runtime;
using Jasper.Transports;

namespace Jasper.ErrorHandling
{
    public class MoveToErrorQueue : IContinuation
    {
        public MoveToErrorQueue(Exception exception)
        {
            Exception = exception;
        }

        public Exception Exception { get; }

        public async Task Execute(IChannelCallback channel, Envelope envelope,
            IExecutionContext execution,
            DateTime utcNow)
        {
            await execution.SendFailureAcknowledgement(envelope,
                $"Moved message {envelope.Id} to the Error Queue.\n{Exception}");

            if (channel is IHasDeadLetterQueue c)
            {
                await c.MoveToErrors(envelope, Exception);
            }
            else
            {
                // If persistable, persist
                await execution.Persistence.MoveToDeadLetterStorage(envelope, Exception);
            }

            execution.Logger.MessageFailed(envelope, Exception);
            execution.Logger.MovedToErrorQueue(envelope, Exception);


        }

        public override string ToString()
        {
            return "Move to Error Queue";
        }

        protected bool Equals(MoveToErrorQueue other)
        {
            return Equals(Exception, other.Exception);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((MoveToErrorQueue) obj);
        }

        public override int GetHashCode()
        {
            return (Exception != null ? Exception.GetHashCode() : 0);
        }
    }
}
