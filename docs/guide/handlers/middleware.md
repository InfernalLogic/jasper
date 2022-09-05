# Middleware

::: tip warning
This whole code compilation model is pretty new and there aren't enough examples yet. Feel very free to ask questions in the Gitter room linked in the top bar of this page.
:::

Jasper supports the "Russian Doll" model of middleware, similar in concept to ASP.NET Core but very different in implementation. Jasper's middleware uses runtime code generation and compilation with [LamarCompiler](https://jasperfx.github.io/lamar/documentation/compilation/). What this means is that "middleware" in Jasper is code that is woven right into the message and route handlers.

As an example, let's say you want to build some custom middleware that is a simple performance timing of either HTTP route execution or message execution. In essence, you want to inject code like this:

<!-- snippet: sample_stopwatch_concept -->
<a id='snippet-sample_stopwatch_concept'></a>
```cs
var stopwatch = new Stopwatch();
stopwatch.Start();
try
{
    // execute the HTTP request
    // or message
}
finally
{
    stopwatch.Stop();
    logger.LogInformation("Ran something in " + stopwatch.ElapsedMilliseconds);
}
```
<sup><a href='https://github.com/JasperFx/alba/blob/master/src/Jasper.Http.Testing/Samples/CustomJasperMiddleware.cs#L134-L148' title='Snippet source file'>snippet source</a> | <a href='#snippet-sample_stopwatch_concept' title='Start of snippet'>anchor</a></sup>
<a id='snippet-sample_stopwatch_concept-1'></a>
```cs
var stopwatch = new Stopwatch();
stopwatch.Start();
try
{
    // execute the HTTP request
    // or message
}
finally
{
    stopwatch.Stop();
    logger.Info("Ran something in " + stopwatch.ElapsedMilliseconds);
}
```
<sup><a href='https://github.com/JasperFx/alba/blob/master/src/Samples/DocumentationSamples/Middleware.cs#L17-L30' title='Snippet source file'>snippet source</a> | <a href='#snippet-sample_stopwatch_concept-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Alright, the first step is to create a LamarCompiler `Frame` class that generates that code around the inner message or HTTP handler:

<!-- snippet: sample_StopwatchFrame -->
<a id='snippet-sample_stopwatchframe'></a>
```cs
public class StopwatchFrame : SyncFrame
{
    private readonly IChain _chain;
    private readonly Variable _stopwatch;
    private Variable _logger;

    public StopwatchFrame(IChain chain)
    {
        _chain = chain;

        // This frame creates a Stopwatch, so we
        // expose that fact to the rest of the generated method
        // just in case someone else wants that
        _stopwatch = new Variable(typeof(Stopwatch), "stopwatch", this);
    }

    public override void GenerateCode(GeneratedMethod method, ISourceWriter writer)
    {
        writer.Write($"var stopwatch = new {typeof(Stopwatch).FullNameInCode()}();");
        writer.Write("stopwatch.Start();");

        writer.Write("BLOCK:try");
        Next?.GenerateCode(method, writer);
        writer.FinishBlock();

        // Write a finally block where you record the stopwatch
        writer.Write("BLOCK:finally");

        writer.Write("stopwatch.Stop();");
        writer.Write(
            $"{_logger.Usage}.Log(Microsoft.Extensions.Logging.LogLevel.Information, \"{_chain.Description} ran in \" + {_stopwatch.Usage}.{nameof(Stopwatch.ElapsedMilliseconds)});)");

        writer.FinishBlock();
    }

    public override IEnumerable<Variable> FindVariables(IMethodVariables chain)
    {
        // This in effect turns into "I need ILogger<IChain> injected into the
        // compiled class"
        _logger = chain.FindVariable(typeof(ILogger<IChain>));
        yield return _logger;
    }
}
```
<sup><a href='https://github.com/JasperFx/alba/blob/master/src/Jasper.Http.Testing/Samples/CustomJasperMiddleware.cs#L17-L62' title='Snippet source file'>snippet source</a> | <a href='#snippet-sample_stopwatchframe' title='Start of snippet'>anchor</a></sup>
<a id='snippet-sample_stopwatchframe-1'></a>
```cs
public class StopwatchFrame : SyncFrame
{
    private readonly IChain _chain;
    private Variable _logger;
    private readonly Variable _stopwatch;

    public StopwatchFrame(IChain chain)
    {
        _chain = chain;

        // This frame creates a Stopwatch, so we
        // expose that fact to the rest of the generated method
        // just in case someone else wants that
        _stopwatch = new Variable(typeof(Stopwatch), "stopwatch", this);
    }

    public override void GenerateCode(GeneratedMethod method, ISourceWriter writer)
    {
        writer.Write($"var stopwatch = new {typeof(Stopwatch).FullNameInCode()}();");
        writer.Write($"stopwatch.Start();");

        writer.Write("BLOCK:try");
        Next?.GenerateCode(method, writer);
        writer.FinishBlock();

        // Write a finally block where you record the stopwatch
        writer.Write("BLOCK:finally");

        writer.Write($"stopwatch.Stop();");
        writer.Write($"{_logger.Usage}.Log(Microsoft.Extensions.Logging.LogLevel.Information, \"{_chain.Description} ran in \" + {_stopwatch.Usage}.{nameof(Stopwatch.ElapsedMilliseconds)});)");

        writer.FinishBlock();
    }

    public override IEnumerable<Variable> FindVariables(IMethodVariables chain)
    {
        // This in effect turns into "I need ILogger<IChain> injected into the
        // compiled class"
        _logger = chain.FindVariable(typeof(ILogger<IChain>));
        yield return _logger;
    }
}
```
<sup><a href='https://github.com/JasperFx/alba/blob/master/src/Samples/DocumentationSamples/Middleware.cs#L35-L79' title='Snippet source file'>snippet source</a> | <a href='#snippet-sample_stopwatchframe-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->



## Applying Middleware

Okay, great, but the next question is "how do I stick this middleware on routes or message handlers?". You've got three options:

1. Use custom attributes
1. Use a custom `IRoutePolicy` or `IHandlerPolicy` class
1. Expose a static `Configure(chain)` method on handler classes

Even though one of the original design goals of FubuMVC and now Jasper was to eliminate or at least reduce the number of attributes users had to spew out into their application code, let's start with using an attribute.

## Custom Attributes

To attach our `StopwatchFrame` as middleware to any route or message handler, we can write a custom attribute based on Jasper's
`ModifyChainAttribute` class as shown below:

<!-- snippet: sample_StopwatchAttribute -->
<a id='snippet-sample_stopwatchattribute'></a>
```cs
public class StopwatchAttribute : ModifyChainAttribute
{
    public override void Modify(IChain chain, GenerationRules rules, IContainer container)
    {
        chain.Middleware.Add(new StopwatchFrame(chain));
    }
}
```
<sup><a href='https://github.com/JasperFx/alba/blob/master/src/Jasper.Http.Testing/Samples/CustomJasperMiddleware.cs#L65-L73' title='Snippet source file'>snippet source</a> | <a href='#snippet-sample_stopwatchattribute' title='Start of snippet'>anchor</a></sup>
<a id='snippet-sample_stopwatchattribute-1'></a>
```cs
public class StopwatchAttribute : ModifyChainAttribute
{
    public override void Modify(IChain chain, GenerationRules rules, IContainer container)
    {
        chain.Middleware.Add(new StopwatchFrame(chain));
    }
}
```
<sup><a href='https://github.com/JasperFx/alba/blob/master/src/Samples/DocumentationSamples/Middleware.cs#L81-L89' title='Snippet source file'>snippet source</a> | <a href='#snippet-sample_stopwatchattribute-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

This attribute can now be placed either on a specific HTTP route endpoint method or message handler method to **only** apply to
that specific action, or it can be placed on a `Handler` or `Endpoint` class to apply to all methods exported by that type.

Here's an example:

<!-- snippet: sample_ClockedEndpoint -->
<a id='snippet-sample_clockedendpoint'></a>
```cs
public class ClockedEndpoint
{
    [Stopwatch]
    public string get_clocked()
    {
        return "how fast";
    }
}
```
<sup><a href='https://github.com/JasperFx/alba/blob/master/src/Jasper.Http.Testing/Samples/CustomJasperMiddleware.cs#L76-L85' title='Snippet source file'>snippet source</a> | <a href='#snippet-sample_clockedendpoint' title='Start of snippet'>anchor</a></sup>
<a id='snippet-sample_clockedendpoint-1'></a>
```cs
public class ClockedEndpoint
{
    [Stopwatch]
    public string get_clocked()
    {
        return "how fast";
    }
}
```
<sup><a href='https://github.com/JasperFx/alba/blob/master/src/Samples/DocumentationSamples/Middleware.cs#L91-L100' title='Snippet source file'>snippet source</a> | <a href='#snippet-sample_clockedendpoint-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Now, when the application is bootstrapped, this is the code that would be generated to handle the "GET /clocked" route:

```
    public class Jasper_Testing_Samples_ClockedEndpoint_get_clocked : Jasper.Http.Model.RouteHandler
    {
        private readonly Microsoft.Extensions.Logging.ILogger<Jasper.Configuration.IChain> _logger;

        public Jasper_Testing_Samples_ClockedEndpoint_get_clocked(Microsoft.Extensions.Logging.ILogger<Jasper.Configuration.IChain> logger)
        {
            _logger = logger;
        }



        public override Task Handle(Microsoft.AspNetCore.Http.HttpContext httpContext, System.String[] segments)
        {
            var clockedEndpoint = new Jasper.Testing.Samples.ClockedEndpoint();
            var stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
            try
            {
                var result_of_get_clocked = clockedEndpoint.get_clocked();
                return WriteText(result_of_get_clocked, httpContext.Response);
            }

            finally
            {
                stopwatch.Stop();
                _logger.Log(Microsoft.Extensions.Logging.LogLevel.Information, "Route 'GET: clocked' ran in " + stopwatch.ElapsedMilliseconds);)
            }

        }

    }
```

`ModifyChainAttribute` is a generic way to add middleware or post processing frames, but if you need to configure things specific to routes or message handlers, you can also use `ModifyHandlerChainAttribute` for message handlers or `ModifyRouteAttribute` for http routes.


## Policies

::: tip warning
Again, please go easy with this feature and try not to shoot yourself in the foot by getting too aggressive with custom policies
:::

You can register user-defined policies that apply to all chains or some subset of chains. For message handlers, implement this interface:

<!-- snippet: sample_IHandlerPolicy -->
<a id='snippet-sample_ihandlerpolicy'></a>
```cs
/// <summary>
///     Use to apply your own conventions or policies to message handlers
/// </summary>
public interface IHandlerPolicy
{
    /// <summary>
    ///     Called during bootstrapping to alter how the message handlers are configured
    /// </summary>
    /// <param name="graph"></param>
    /// <param name="rules"></param>
    /// <param name="container">The application's underlying Lamar Container</param>
    void Apply(HandlerGraph graph, GenerationRules rules, IContainer container);
}
```
<sup><a href='https://github.com/JasperFx/alba/blob/master/src/Jasper/Configuration/IHandlerPolicy.cs#L7-L23' title='Snippet source file'>snippet source</a> | <a href='#snippet-sample_ihandlerpolicy' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Here's a simple sample that registers middleware on each handler chain:

<!-- snippet: sample_WrapWithSimple -->
<a id='snippet-sample_wrapwithsimple'></a>
```cs
public class WrapWithSimple : IHandlerPolicy
{
    public void Apply(HandlerGraph graph, GenerationRules rules, IContainer container)
    {
        foreach (var chain in graph.Chains) chain.Middleware.Add(new SimpleWrapper());
    }
}
```
<sup><a href='https://github.com/JasperFx/alba/blob/master/src/Jasper.Testing/BootstrappingSamples.cs#L27-L37' title='Snippet source file'>snippet source</a> | <a href='#snippet-sample_wrapwithsimple' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Then register your custom `IHandlerPolicy` with a Jasper application like this:

<!-- snippet: sample_AppWithHandlerPolicy -->
<a id='snippet-sample_appwithhandlerpolicy'></a>
```cs
using var host = await Host.CreateDefaultBuilder()
    .UseJasper(opts =>
    {
        opts.Handlers.GlobalPolicy<WrapWithSimple>();
    }).StartAsync();
```
<sup><a href='https://github.com/JasperFx/alba/blob/master/src/Jasper.Testing/BootstrappingSamples.cs#L15-L23' title='Snippet source file'>snippet source</a> | <a href='#snippet-sample_appwithhandlerpolicy' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Using Configure(chain) Methods

::: tip warning
This feature is experimental, but is meant to provide an easy way to apply middleware or other configuration to specific HTTP endpoints or
message handlers without writing custom policies or having to resort to all new attributes.
:::

There's one last option for configuring chains by a naming convention. If you want to configure the chains from just one handler or endpoint class,
you can implement a method with one of these signatures:

```
public static void Configure(IChain)
{
    // gets called for each endpoint or message handling method
    // on just this class
}

public static void Configure(RouteChain chain)`
{
    // gets called for each endpoint method on this class
}

public static void Configure(HandlerChain chain)
{
    // gets called for each message handling method
    // on just this class
}
```

Here's an example of this being used from Jasper's test suite:

<!-- snippet: sample_customized_handler_using_Configure -->
<a id='snippet-sample_customized_handler_using_configure'></a>
```cs
public class CustomizedHandler
{
    public void Handle(SpecialMessage message)
    {
        // actually handle the SpecialMessage
    }

    public static void Configure(HandlerChain chain)
    {
        chain.Middleware.Add(new CustomFrame());
    }
}
```
<sup><a href='https://github.com/JasperFx/alba/blob/master/src/Jasper.Testing/Configuration/can_customize_handler_chain_through_Configure_call_on_HandlerType.cs#L24-L37' title='Snippet source file'>snippet source</a> | <a href='#snippet-sample_customized_handler_using_configure' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->



