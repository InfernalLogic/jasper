using System;
using System.Collections.Generic;
using System.Linq;
using Baseline;
using Baseline.ImTools;
using Baseline.Reflection;
using Jasper.Attributes;
using Jasper.Configuration;
using Jasper.Transports;
using Jasper.Transports.Sending;

namespace Jasper.Runtime.Routing;

internal abstract class MessageRouterBase<T> : IMessageRouter<T>, IMessageRouter
{
    internal JasperRuntime Runtime { get; }

    private ImHashMap<Uri, MessageRoute> _specificRoutes = ImHashMap<Uri, MessageRoute>.Empty;
    private ImHashMap<string, MessageRoute> _localRoutes = ImHashMap<string, MessageRoute>.Empty;

    private readonly MessageRoute[] _topicRoutes;

    private readonly MessageRoute _local;

    protected MessageRouterBase(JasperRuntime runtime)
    {
        // We'll use this for executing scheduled envelopes that aren't native
        LocalDurableQueue = runtime.GetOrBuildSendingAgent(TransportConstants.DurableLocalUri);

        var chain = runtime.Handlers.ChainFor(typeof(T));
        if (chain != null)
        {
            var handlerRules = chain.Handlers.SelectMany(x => x.Method.GetAllAttributes<ModifyEnvelopeAttribute>())
                .OfType<IEnvelopeRule>();
            HandlerRules.AddRange(handlerRules);
        }

        _local = new MessageRoute(typeof(T), runtime.DetermineLocalSendingAgent(typeof(T)).Endpoint);
        _local.Rules.AddRange(HandlerRules!);

        _topicRoutes = runtime.Options.AllEndpoints().Where(x => x.RoutingType == RoutingMode.ByTopic)
            .Select(endpoint => new MessageRoute(typeof(T), endpoint)).ToArray();

        Runtime = runtime;
    }

    public ISendingAgent LocalDurableQueue { get; }

    public List<IEnvelopeRule> HandlerRules { get; } = new();

    public abstract Envelope[] RouteForSend(T message, DeliveryOptions? options);
    public abstract Envelope[] RouteForPublish(T message, DeliveryOptions? options);

    public Envelope RouteToDestination(T message, Uri uri, DeliveryOptions? options)
    {
        if (message == null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        if (_specificRoutes.TryFind(uri, out var route))
        {
            return route.CreateForSending(message, options, LocalDurableQueue, Runtime);
        }

        var agent = Runtime.GetOrBuildSendingAgent(uri);
        route = new MessageRoute(message.GetType(), agent.Endpoint);
        _specificRoutes = _specificRoutes.AddOrUpdate(uri, route);

        return route.CreateForSending(message, options, LocalDurableQueue, Runtime);
    }

    public Envelope RouteToEndpointByName(T message, string endpointName, DeliveryOptions? options)
    {
        // Cache a message route by endpoint name
        throw new NotImplementedException();
    }

    public Envelope RouteLocal(T message, DeliveryOptions? options)
    {
        if (message == null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        return _local.CreateForSending(message, options, LocalDurableQueue, Runtime);
    }

    public Envelope[] RouteToTopic(T message, string topicName, DeliveryOptions? options)
    {
        if (message == null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        if (!_topicRoutes.Any()) throw new InvalidOperationException("There are no registered topic routed endpoints");
        var envelopes = new Envelope[_topicRoutes.Length];
        for (int i = 0; i < envelopes.Length; i++)
        {
            envelopes[i] = _topicRoutes[i].CreateForSending(message, options, LocalDurableQueue, Runtime);
            envelopes[i].TopicName = topicName;
        }

        return envelopes;
    }

    public Envelope RouteLocal(T message, string workerQueue, DeliveryOptions? options)
    {
        if (message == null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        workerQueue = workerQueue.ToLowerInvariant();
        if (_localRoutes.TryFind(workerQueue, out var route))
        {
            return route.CreateForSending(message, options, LocalDurableQueue, Runtime);
        }

        var queue = Runtime.AgentForLocalQueue(workerQueue);
        route = new MessageRoute(typeof(T), queue.Endpoint);
        route.Rules.AddRange(HandlerRules);
        _localRoutes = _localRoutes.AddOrUpdate(workerQueue, route);

        return route.CreateForSending(message, options, LocalDurableQueue, Runtime);
    }

    public Envelope[] RouteForSend(object message, DeliveryOptions? options)
    {
        return RouteForSend((T)message, options);
    }

    public Envelope[] RouteForPublish(object message, DeliveryOptions? options)
    {
        return RouteForPublish((T)message, options);
    }

    public Envelope RouteToDestination(object message, Uri uri, DeliveryOptions? options)
    {
        return RouteToDestination((T)message, uri, options);
    }

    public Envelope RouteToEndpointByName(object message, string endpointName, DeliveryOptions? options)
    {
        return RouteToEndpointByName((T)message, endpointName, options);
    }

    public Envelope[] RouteToTopic(object message, string topicName, DeliveryOptions? options)
    {
        return RouteToTopic((T)message, topicName, options);
    }

    public Envelope RouteLocal(object message, DeliveryOptions? options)
    {
        return RouteLocal((T)message, options);
    }

    public Envelope RouteLocal(object message, string workerQueue, DeliveryOptions? options)
    {
        return RouteLocal((T)message, workerQueue, options);
    }
}
