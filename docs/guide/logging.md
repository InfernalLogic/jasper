# Logging, Diagnostics, and Metrics

Jasper logs through the standard .NET `ILogger` abstraction, and there's nothing special you need to do
to enable that logging other than using one of the standard approaches for bootstrapping a .NET application
using `IHostBuilder`. Jasper is logging all messages sent, received, and executed inline.


## Open Telemetry

Jasper also supports the [Open Telemetry](https://opentelemetry.io/docs/instrumentation/net/) standard for distributed tracing. To enable
the collection of Open Telemetry data, you need to add Jasper as a data source as shown in this
code sample:

<!-- snippet: sample_enabling_open_telemetry -->
<a id='snippet-sample_enabling_open_telemetry'></a>
```cs
// builder.Services is an IServiceCollection object
builder.Services.AddOpenTelemetryTracing(x =>
{
    x.SetResourceBuilder(ResourceBuilder
            .CreateDefault()
            .AddService("OtelWebApi")) // <-- sets service name

        .AddJaegerExporter()
        .AddAspNetCoreInstrumentation()

        // This is absolutely necessary to collect the Jasper
        // open telemetry tracing information in your application
        .AddSource("Jasper");
});
```
<sup><a href='https://github.com/JasperFx/alba/blob/master/src/opentelemetry/OtelWebApi/Program.cs#L37-L54' title='Snippet source file'>snippet source</a> | <a href='#snippet-sample_enabling_open_telemetry' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->
