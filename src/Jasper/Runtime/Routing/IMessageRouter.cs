using System;

namespace Jasper.Runtime.Routing;

public interface IMessageRouter
{
    Envelope[] RouteForSend(object message, DeliveryOptions? options);
    Envelope[] RouteForPublish(object message, DeliveryOptions? options);
    Envelope RouteToDestination(object message, Uri uri, DeliveryOptions? options);
    Envelope RouteToEndpointByName(object message, string endpointName, DeliveryOptions? options);
    Envelope[] RouteToTopic(object message, string topicName, DeliveryOptions? options);
    Envelope RouteLocal(object message, DeliveryOptions? options);
    Envelope RouteLocal(object message, string workerQueue, DeliveryOptions? options);
}

