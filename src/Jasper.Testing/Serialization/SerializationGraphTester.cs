﻿using System.Text;
using Jasper.Serialization;
using Newtonsoft.Json;
using Shouldly;
using TestMessages;
using Xunit;

namespace Jasper.Testing.Serialization
{
    public class SerializationGraphTester
    {
        [Fact]
        public void can_try_to_deserialize_an_envelope_with_no_message_type()
        {
            var serialization = MessagingSerializationGraph.Basic();

            var json = JsonConvert.SerializeObject(new Message2(), new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All
            });

            var envelope = new Envelope
            {
                Data = Encoding.UTF8.GetBytes(json),
                MessageType = null,
                ContentType = EnvelopeConstants.JsonContentType
            };

            serialization.Deserialize(envelope)
                .ShouldBeOfType<Message2>();
        }

        [Fact]
        public void can_try_to_deserialize_an_envelope_with_no_message_type_or_content_type()
        {
            var serialization = MessagingSerializationGraph.Basic();

            var json = JsonConvert.SerializeObject(new Message2(), new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All
            });

            var envelope = new Envelope
            {
                Data = Encoding.UTF8.GetBytes(json),
                MessageType = null,
                ContentType = null
            };

            serialization.Deserialize(envelope)
                .ShouldBeOfType<Message2>();
        }
    }
}
