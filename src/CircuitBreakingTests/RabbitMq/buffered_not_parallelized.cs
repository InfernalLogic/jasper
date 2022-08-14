using Baseline.Dates;
using Jasper;
using Jasper.ErrorHandling;
using Jasper.RabbitMQ;
using Xunit.Abstractions;

namespace CircuitBreakingTests.RabbitMq;

public class buffered_not_parallelized : CircuitBreakerIntegrationContext
{
    public buffered_not_parallelized(ITestOutputHelper output) : base(output)
    {
    }

    protected override void configureListener(JasperOptions opts)
    {
        // Requeue failed messages.
        opts.Handlers.OnException<BadImageFormatException>().Or<DivideByZeroException>()
            .Requeue();

        opts.PublishAllMessages().ToRabbitQueue("circuit2");
        opts.ListenToRabbitQueue("circuit2").CircuitBreaker(cb =>
        {
            cb.MinimumThreshold = 250;
            cb.PauseTime = 10.Seconds();
            cb.TrackingPeriod = 1.Minutes();
            cb.FailurePercentageThreshold = 20;
        }).BufferedInMemory();
    }
}