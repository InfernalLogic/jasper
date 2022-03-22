﻿using System;
using System.Collections.Generic;
using Jasper.Transports;

namespace Jasper.Logging
{
    // SAMPLE: ITransportLogger
    public interface ITransportLogger
    {
        /// <summary>
        ///     Catch all hook for any exceptions encountered by the messaging
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="correlationId"></param>
        /// <param name="message"></param>
        void LogException(Exception? ex, object? correlationId = null, string message = "Exception detected:");

        /// <summary>
        ///     An outgoing batch of messages were sent successfully
        /// </summary>
        /// <param name="batch"></param>
        void OutgoingBatchSucceeded(OutgoingMessageBatch batch);

        /// <summary>
        ///     An outgoing batch of messages were sent unsuccessfully
        /// </summary>
        /// <param name="batch"></param>
        /// <param name="ex"></param>
        void OutgoingBatchFailed(OutgoingMessageBatch batch, Exception? ex = null);

        /// <summary>
        ///     An incoming batch of messages was received successfully
        /// </summary>
        /// <param name="envelopes"></param>
        void IncomingBatchReceived(IEnumerable<Envelope?> envelopes);

        /// <summary>
        ///     The sending agent for this destination experienced too many failures and has been latched
        /// </summary>
        /// <param name="destination"></param>
        void CircuitBroken(Uri? destination);

        /// <summary>
        ///     The sending agent for this destination has been successfully pinged and un-latched
        /// </summary>
        /// <param name="destination"></param>
        void CircuitResumed(Uri? destination);

        /// <summary>
        ///     Scheduled jobs were queued for execution
        /// </summary>
        /// <param name="envelopes"></param>
        void ScheduledJobsQueuedForExecution(IEnumerable<Envelope?> envelopes);

        /// <summary>
        ///     Incoming messages were recovered from storage
        /// </summary>
        /// <param name="envelopes"></param>
        void RecoveredIncoming(IEnumerable<Envelope?> envelopes);

        /// <summary>
        ///     Outgoing messages were recovered from storage
        /// </summary>
        /// <param name="envelopes"></param>
        void RecoveredOutgoing(IEnumerable<Envelope?> envelopes);

        /// <summary>
        ///     Outgoing envelopes are discarded because their DeliverBy has expired
        /// </summary>
        /// <param name="envelopes"></param>
        void DiscardedExpired(IEnumerable<Envelope?> envelopes);

        /// <summary>
        ///     Logged when the outgoing message recovery finds envelopes with a Destination
        ///     that cannot be resolved to a known transport in the system
        /// </summary>
        /// <param name="envelopes"></param>
        void DiscardedUnknownTransport(IEnumerable<Envelope> envelopes);


        /// <summary>
        ///     Called whenever the current node changes its listening status
        ///     to either accepting or too busy
        /// </summary>
        /// <param name="status"></param>
        void ListeningStatusChange(ListeningStatus status);

        void IncomingReceived(Envelope? envelope);
    }

    // ENDSAMPLE
}
