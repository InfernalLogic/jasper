﻿using System;
using System.Linq;
using Jasper.Transports;
using Jasper.Transports.Local;
using Jasper.Util;
using Shouldly;
using Xunit;

namespace Jasper.Testing.Transports.Local
{
    public class LocalTransportTests
    {
        [Theory]
        [InlineData(TransportConstants.Default)]
        [InlineData(TransportConstants.Replies)]
        public void has_default_queues(string queueName)
        {
            new LocalTransport()
                .AllQueues().Any(x => x.Name == queueName)
                .ShouldBeTrue();
        }

        [Fact]
        public void reply_endpoint_is_replies()
        {
            new LocalTransport()
                .ReplyEndpoint()
                .Uri.ShouldBe(TransportConstants.RepliesUri);
        }

        [Fact]
        public void LocalQueueSettings_forces_the_queue_name_to_be_lower_case()
        {
            new LocalQueueSettings("Foo")
                .Name.ShouldBe("foo");
        }



        [Fact]
        public void case_insensitive_queue_find()
        {
            var transport = new LocalTransport();

            transport.QueueFor("One")
                .ShouldBeSameAs(transport.QueueFor("one"));
        }


        [Fact]
        public void queue_at_extension()
        {
            var uri = LocalTransport.AtQueue(TransportConstants.LocalUri, (string) "one");

            LocalTransport.QueueName(uri).ShouldBe("one");
        }

        [Fact]
        public void queue_at_extension_durable()
        {
            var uri = LocalTransport.AtQueue(TransportConstants.DurableLocalUri, (string) "one");

            LocalTransport.QueueName(uri).ShouldBe("one");
        }


        [Fact]
        public void queue_at_other_queue()
        {
            var uri = LocalTransport.AtQueue("tcp://localhost:2222".ToUri(), (string) "one");

            LocalTransport.QueueName(uri).ShouldBe("one");
        }

        [Fact]
        public void fall_back_to_the_default_queue_if_no_segments()
        {
            LocalTransport.QueueName("tcp://localhost:2222".ToUri()).ShouldBe(TransportConstants.Default);
        }

        [Fact]
        public void negative_case_with_local()
        {
            "local://default".ToUri().IsDurable().ShouldBeFalse();
            "local://replies".ToUri().IsDurable().ShouldBeFalse();
        }

        [Fact]
        public void negative_case_with_tcp()
        {
            "tcp://localhost:2200".ToUri().IsDurable().ShouldBeFalse();
            "tcp://localhost:2200/replies".ToUri().IsDurable().ShouldBeFalse();
        }

        [Fact]
        public void positive_case_with_local()
        {
            "local://durable".ToUri().IsDurable().ShouldBeTrue();
            "local://durable/replies".ToUri().IsDurable().ShouldBeTrue();
        }

        [Fact]
        public void positive_case_with_tcp()
        {
            "tcp://localhost:2200/durable".ToUri().IsDurable().ShouldBeTrue();
            "tcp://localhost:2200/durable/replies".ToUri().IsDurable().ShouldBeTrue();
        }

        [Fact]
        public void still_get_queue_name_with_durable()
        {
            LocalTransport.QueueName("tcp://localhost:2222/durable".ToUri()).ShouldBe(TransportConstants.Default);
            LocalTransport.QueueName("tcp://localhost:2222/durable/incoming".ToUri()).ShouldBe("incoming");
        }

        [Fact]
        public void use_the_last_segment_if_it_exists()
        {
            LocalTransport.QueueName("tcp://localhost:2222/incoming".ToUri()).ShouldBe("incoming");
        }


    }
}
