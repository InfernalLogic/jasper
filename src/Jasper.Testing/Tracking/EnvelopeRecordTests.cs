using System.Diagnostics;
using Jasper.Testing.Messaging;
using Jasper.Tracking;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Shouldly;
using Xunit;

namespace Jasper.Testing.Tracking;

public class EnvelopeRecordTests
{
    [Fact]
    public void creating_a_new_envelope_record_records_otel_activity()
    {
        using var source = new ActivitySource("Testing");

        using var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddSource("Testing")
            .SetResourceBuilder(
                ResourceBuilder.CreateDefault()
                    .AddService(serviceName: "Jasper", serviceVersion: "1.0"))
            .AddConsoleExporter()
            .Build();

        var root = source.CreateActivity("process", ActivityKind.Internal);
        root.Start();

        var parent = source.CreateActivity("process", ActivityKind.Internal);
        parent.Start();

        var child = source.CreateActivity("process", ActivityKind.Internal);
        child.Start();

        root.ShouldNotBeNull();
        parent.ShouldNotBeNull();
        child.ShouldNotBeNull();

        var record = new EnvelopeRecord(EventType.Sent, ObjectMother.Envelope(), 1000, null);

        root.Id.ShouldContain(record.RootId);
        record.ParentId.ShouldContain(parent.Id);
        record.ActivityId.ShouldBe(child.Id);

        root.Stop();
        parent.Stop();
        child.Stop();
    }
}
