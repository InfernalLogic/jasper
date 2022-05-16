using System;
using System.Linq;
using System.Threading.Tasks;
using Jasper.Attributes;
using Jasper.Runtime;
using Jasper.Testing.Configuration;
using Jasper.Tracking;
using Jasper.Transports.Tcp;
using Jasper.Util;
using Shouldly;
using TestMessages;
using Xunit;

namespace Jasper.Testing.Acceptance
{
    public class overriding_delivery_options_when_sending : SendingContext
    {
        [Fact]
        public void apply_message_type_rules_from_attributes()
        {
            var envelope = theSendingRuntime.RoutingFor(typeof(MessageWithSpecialAttribute))
                .RouteLocal(new MessageWithSpecialAttribute(), null);

            envelope.Headers["special"].ShouldBe("true");
        }


        [Fact]
        public void deliver_by_mechanics()
        {
            var envelope = theSendingRuntime.RoutingFor(typeof(MessageWithSpecialAttribute))
                .RouteLocal(new MessageWithSpecialAttribute(), null);

            envelope.DeliverBy.Value.ShouldBeGreaterThan(DateTimeOffset.UtcNow);
        }


        [Fact]
        public async Task honor_customization_attributes_on_message_type()
        {
            var session = await theSender.TrackActivity()
                .IncludeExternalTransports()
                .AlsoTrack(theReceiver)
                .SendMessageAndWaitAsync(new MessageWithSpecialAttribute());

            var outgoing = session
                .FindSingleReceivedEnvelopeForMessageType<MessageWithSpecialAttribute>();

            outgoing.Headers["special"].ShouldBe("true");
        }

        [Fact]
        public void message_type_rules_override_endpoint_rules    ()
        {
            SenderOptions(opts =>
            {
                opts.PublishMessage<MessageWithSpecialAttribute>()
                    .ToPort(ReceiverPort)
                    .CustomizeOutgoing(e => e.Headers["special"] = "different");
            });

            var outgoing = theSendingRuntime
                .RouteForSend(new MessageWithSpecialAttribute(), null).Single();

            outgoing.Headers["special"].ShouldBe("true");
        }

        [Fact]
        public void delivery_options_trumps_all_other_rules()
        {
            SenderOptions(opts =>
            {
                opts.PublishMessage<MessageWithSpecialAttribute>()
                    .ToPort(ReceiverPort)
                    .CustomizeOutgoing(e => e.Headers["special"] = "different");
            });

            var outgoing = theSendingRuntime
                .RouteForSend(new MessageWithSpecialAttribute(), new DeliveryOptions().WithHeader("special", "explicit")).Single();

            outgoing.Headers["special"].ShouldBe("explicit");
        }

        [Fact]
        public async Task can_use_delivery_options_on_send()
        {
            var session = await theSender
                .TrackActivity()
                .IncludeExternalTransports()
                .AlsoTrack(theReceiver)
                .SendMessageAndWaitAsync(new StatusMessage(), new DeliveryOptions
                {
                    AckRequested = true
                });

            var envelope = session.FindSingleReceivedEnvelopeForMessageType<StatusMessage>();
            envelope.AckRequested.ShouldBeTrue();
        }

        [Fact]
        public async Task can_use_delivery_options_on_send_to_specific()
        {
            var uri = $"tcp://localhost:{ReceiverPort}".ToUri();

            var session = await theSender
                .TrackActivity()
                .IncludeExternalTransports()
                .AlsoTrack(theReceiver)
                .SendMessageAndWaitAsync(uri, new StatusMessage(), new DeliveryOptions
                {
                    AckRequested = true
                });

            var envelope = session.FindSingleReceivedEnvelopeForMessageType<StatusMessage>();
            envelope.AckRequested.ShouldBeTrue();
        }

        [Fact]
        public async Task can_use_delivery_options_on_publish()
        {
            var session = await theSender
                .TrackActivity()
                .IncludeExternalTransports()
                .AlsoTrack(theReceiver)
                .PublishMessageAndWaitAsync(new StatusMessage(), new DeliveryOptions
                {
                    AckRequested = true
                });

            var envelope = session.FindSingleReceivedEnvelopeForMessageType<StatusMessage>();
            envelope.AckRequested.ShouldBeTrue();
        }

        /* TODO



         6. SchedulePublishAsync(message, time) with optional DeliveryOptions
         7> SchedulePublishAsync(message, delay) with optional DeliveryOptions
         8. DeliveryOptions on SendAsync w/ endpoint config
         9. DeliveryOptions on PublishAsync w/ endpoint config

         12. SendAsync(uri, message) with optional DeliveryOptions w/ endpoint config
         13. SchedulePublishAsync(message, time) with optional DeliveryOptions w/ endpoint config
         14. SchedulePublishAsync(message, delay) with optional DeliveryOptions w/ endpoint config






         */
    }

    public class SpecialAttribute : ModifyEnvelopeAttribute
    {
        public override void Modify(Envelope envelope)
        {
            envelope.Headers["special"] = "true";
        }
    }

    [Special]
    [DeliverWithin(5)]
    public class MessageWithSpecialAttribute
    {
    }

    #region sample_UsingDeliverWithinAttribute
    // Any message of this type should be successfully
    // delivered within 10 seconds or discarded
    [DeliverWithin(10)]
    public class StatusMessage
    {
    }

    #endregion

    public class StatusMessageHandler
    {
        public void Handle(StatusMessage message)
        {

        }
    }

    public class MySpecialMessageHandler
    {
        public void Handle(MessageWithSpecialAttribute message){}
    }

}
