﻿using Baseline.Dates;
using Jasper;
using Jasper.ErrorHandling;
using Jasper.Tcp;
using Microsoft.Extensions.Hosting;
using TestMessages;

namespace Samples
{
    // SAMPLE: configuring-messaging-with-JasperOptions
    public class MyMessagingApp : JasperOptions
    {
        public MyMessagingApp()
        {
            // Configure handler policies
            Handlers
                .OnException<SqlException>()
                .RetryLater(3.Seconds());

            // Declare published messages
            Endpoints.Publish(x =>
            {
                x.Message<Message1>();
                x.ToServerAndPort("server1", 2222);
            });

            // Configure the built in transports
            Endpoints.ListenAtPort(2233);
        }
    }
    // ENDSAMPLE


    // SAMPLE: MyListeningApp
    public class MyListeningApp : JasperOptions
    {
        public MyListeningApp()
        {
            // Use the simpler, but transport specific syntax
            // to just declare what port the transport should use
            // to listen for incoming messages
            Endpoints.ListenAtPort(2233);
        }
    }
    // ENDSAMPLE


    // SAMPLE: LightweightTransportApp
    public class LightweightTransportApp : JasperOptions
    {
        public LightweightTransportApp()
        {
            // Set up a listener (this is optional)
            Endpoints.ListenAtPort(4000);

            Endpoints.Publish(x =>
            {
                x.Message<Message2>()
                    .ToServerAndPort("remoteserver", 2201);
            });
        }
    }
    // ENDSAMPLE

    // SAMPLE: DurableTransportApp
    public class DurableTransportApp : JasperOptions
    {
        public DurableTransportApp()
        {
            Endpoints
                .PublishAllMessages()
                .ToServerAndPort("server1", 2201)

                // This applies the store and forward persistence
                // to the outgoing message
                .Durably();

            // Set up a listener (this is optional)
            Endpoints.ListenAtPort(2200)

                // This applies the message persistence
                // to the incoming endpoint such that incoming
                // messages are first saved to the application
                // database before attempting to handle the
                // incoming message
                .DurablyPersistedLocally();

        }
    }
    // ENDSAMPLE


    // SAMPLE: LocalTransportApp
    public class LocalTransportApp : JasperOptions
    {
        public LocalTransportApp()
        {
            // Publish the message Message2 the "important"
            // local queue
            Endpoints.Publish(x =>
            {
                x.Message<Message2>();
                x.ToLocalQueue("important");
            });
        }
    }

    // ENDSAMPLE

    // SAMPLE: LocalDurableTransportApp
    public class LocalDurableTransportApp : JasperOptions
    {
        public LocalDurableTransportApp()
        {
            // Make the default local queue durable
            Endpoints.DefaultLocalQueue.DurablyPersistedLocally();

            // Or do just this by name
            Endpoints
                .LocalQueue("important")
                .DurablyPersistedLocally();
        }
    }

    // ENDSAMPLE


    public class Samples
    {
        public void Go()
        {
            // SAMPLE: using-configuration-with-jasperoptions
            var host = Host.CreateDefaultBuilder()
                .UseJasper()
                .Start();

            // ENDSAMPLE
        }

    }
}
