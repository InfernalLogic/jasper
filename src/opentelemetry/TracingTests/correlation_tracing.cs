using Baseline.Dates;
using Jasper;
using Jasper.Tracking;
using OtelMessages;
using TracingTests;
using Xunit.Abstractions;
using Shouldly;

[Collection("otel")]
public class correlation_tracing : IClassFixture<HostsFixture>, IAsyncLifetime
{
    private readonly HostsFixture _fixture;
    private readonly ITestOutputHelper _output;
    private ITrackedSession theSession;
    private Envelope theOriginalEnvelope;

    public correlation_tracing(HostsFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    public async Task InitializeAsync()
    {
        theSession = await _fixture.WebApi
            .TrackActivity()
            .AlsoTrack(_fixture.FirstSubscriber)
            .AlsoTrack(_fixture.SecondSubscriber)
            .Timeout(1.Minutes())
            .ExecuteAndWaitAsync(c =>
            {
                return _fixture.WebApi.Scenario(x =>
                {
                    x.Post.Json(new InitialPost("Byron Scott")).ToUrl("/invoke");
                });
            });

        foreach (var @record in theSession.AllRecordsInOrder().Where(x => x.EventType == EventType.MessageSucceeded))
        {
            _output.WriteLine(@record.ToString());
        }

        theOriginalEnvelope = theSession.FindSingleExecutedEnvelopeForMessageType<InitialCommand>();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public void can_find_the_initial_command()
    {
        theOriginalEnvelope.ShouldNotBeNull();
    }

    [Fact]
    public void tracing_from_invoke_to_other_invoke()
    {
        var envelope = theSession.FindSingleExecutedEnvelopeForMessageType<LocalMessage1>();

        envelope.CorrelationId.ShouldBe(theOriginalEnvelope.CorrelationId);
        envelope.Source.ShouldBe("WebApi");
        envelope.CausationId.ShouldBe(theOriginalEnvelope.Id.ToString());
    }

    [Fact]
    public void tracing_from_invoke_to_enqueue()
    {
        var envelope = theSession.FindSingleExecutedEnvelopeForMessageType<LocalMessage2>();

        envelope.CorrelationId.ShouldBe(theOriginalEnvelope.CorrelationId);
        envelope.Source.ShouldBe("WebApi");
        envelope.CausationId.ShouldBe(theOriginalEnvelope.Id.ToString());
    }

    [Fact]
    public void trace_through_tcp()
    {
        var envelope = theSession.FindSingleReceivedEnvelopeForMessageType<TcpMessage1>();

        envelope.CorrelationId.ShouldBe(theOriginalEnvelope.CorrelationId);
        envelope.Source.ShouldBe("WebApi");
        envelope.CausationId.ShouldBe(theOriginalEnvelope.Id.ToString());
    }

    [Fact]
    public void trace_through_tcp_and_back_via_tcp()
    {
        var envelope1 = theSession.FindSingleReceivedEnvelopeForMessageType<TcpMessage1>();
        var envelope2 = theSession.FindSingleReceivedEnvelopeForMessageType<TcpMessage2>();

        envelope2.Source.ShouldBe("Subscriber1");
        envelope2.CorrelationId.ShouldBe(theOriginalEnvelope.CorrelationId);
        envelope2.CausationId.ShouldBe(envelope1.Id.ToString());
    }

    [Fact]
    public void trace_through_rabbit()
    {
        var envelopes = theSession.FindEnvelopesWithMessageType<RabbitMessage1>()
            .Where(x => x.EventType == EventType.MessageSucceeded)
            .Select(x => x.Envelope)
            .OrderBy(x => x.Source)
            .ToArray();

        var atSubscriber1 = envelopes[0];
        var atSubscriber2 = envelopes[1];

        atSubscriber1.Source.ShouldBe("WebApi");
        atSubscriber2.Source.ShouldBe("WebApi");

        atSubscriber1.CorrelationId.ShouldBe(theOriginalEnvelope.CorrelationId);
        atSubscriber2.CorrelationId.ShouldBe(theOriginalEnvelope.CorrelationId);

        atSubscriber1.CausationId.ShouldBe(theOriginalEnvelope.Id.ToString());
        atSubscriber2.CausationId.ShouldBe(theOriginalEnvelope.Id.ToString());
    }

    [Fact]
    public void rabbit_to_rabbit_tracing()
    {
        var envelopes = theSession.FindEnvelopesWithMessageType<RabbitMessage1>()
            .Where(x => x.EventType == EventType.MessageSucceeded)
            .Select(x => x.Envelope)
            .OrderBy(x => x.Source)
            .ToArray();


        var atSubscriber2 = envelopes[1];
        var rabbit2 = theSession.FindSingleReceivedEnvelopeForMessageType<RabbitMessage2>();

        rabbit2.CorrelationId.ShouldBe(theOriginalEnvelope.CorrelationId);
        rabbit2.CausationId.ShouldBe(atSubscriber2.Id.ToString());
        rabbit2.Source.ShouldBe("Subscriber1");

    }
}
