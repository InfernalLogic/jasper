// <auto-generated/>
#pragma warning disable
using Jasper.Persistence.Marten.Publishing;

namespace Internal.Generated.JasperHandlers
{
    // START: IncrementDHandler1615861234
    public class IncrementDHandler1615861234 : Jasper.Runtime.Handlers.MessageHandler
    {
        private readonly Jasper.Persistence.Marten.Publishing.OutboxedSessionFactory _outboxedSessionFactory;

        public IncrementDHandler1615861234(Jasper.Persistence.Marten.Publishing.OutboxedSessionFactory outboxedSessionFactory)
        {
            _outboxedSessionFactory = outboxedSessionFactory;
        }



        public override async System.Threading.Tasks.Task HandleAsync(Jasper.IMessageContext context, System.Threading.CancellationToken cancellation)
        {
            var letterHandler = new Jasper.Persistence.Testing.Marten.LetterHandler();
            var incrementD = (Jasper.Persistence.Testing.Marten.IncrementD)context.Envelope.Message;
            await using var documentSession = _outboxedSessionFactory.OpenSession(context);
            var eventStore = documentSession.Events;
            // Loading Marten aggregate
            var eventStream = await eventStore.FetchForWriting<Jasper.Persistence.Testing.Marten.LetterAggregate>(incrementD.LetterAggregateId, cancellation).ConfigureAwait(false);

            await letterHandler.Handle(incrementD, eventStream).ConfigureAwait(false);
            await documentSession.SaveChangesAsync(cancellation).ConfigureAwait(false);
        }

    }

    // END: IncrementDHandler1615861234
    
    
}

