using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Baseline.Dates;
using Jasper.ErrorHandling.Matches;
using Jasper.Transports;
using Jasper.Transports.Util;

namespace Jasper.ErrorHandling;

internal class CircuitBreaker : IDisposable
{
    private readonly CircuitBreakerOptions _options;
    private readonly IExceptionMatch _match;
    private readonly IListeningAgent _listeningAgent;
    private readonly CancellationTokenSource _cancellation = new CancellationTokenSource();
    private readonly ActionBlock<object[]> _processingBlock;
    private readonly BatchingBlock<object> _batching;
    private readonly List<Generation> _generations = new();
    private readonly double _ratio;

    public CircuitBreaker(CircuitBreakerOptions options, IExceptionMatch match, IListeningAgent listeningAgent)
    {
        _options = options;
        _match = match;
        _listeningAgent = listeningAgent;

        _processingBlock = new ActionBlock<object[]>(processExceptionsAsync);
        _batching = new BatchingBlock<object>(options.SamplingPeriod, _processingBlock);

        GenerationPeriod = ((int)Math.Floor(_options.TrackingPeriod.TotalSeconds / 4)).Seconds();

        _ratio = _options.FailurePercentageThreshold / 100.0;
    }

    public TimeSpan GenerationPeriod { get; set; }


    public bool ShouldStopProcessing()
    {
        var failures = _generations.Sum(x => x.Failures);
        var totals = _generations.Sum(x => x.Total);

        if (totals < _options.MinimumThreshold) return false;

        return (((double)failures) / ((double)totals) >= _ratio);
    }

    private Task processExceptionsAsync(object[] tokens)
    {
        return ProcessExceptionsAsync(DateTimeOffset.UtcNow, tokens).AsTask();
    }

    public ValueTask ProcessExceptionsAsync(DateTimeOffset time, object[] tokens)
    {
        var failures = tokens.OfType<Exception>().Count(x => _match.Matches(x));

        return UpdateTotalsAsync(time, failures, tokens.Length);
    }

    public async ValueTask UpdateTotalsAsync(DateTimeOffset time, int failures, int total)
    {
        var generation = DetermineGeneration(time);
        generation.Failures += failures;
        generation.Total += total;

        if (failures > 0 && ShouldStopProcessing())
        {
            await _listeningAgent.PauseAsync(_options.PauseTime);
        }
    }

    public Generation DetermineGeneration(DateTimeOffset now)
    {
        _generations.RemoveAll(x => x.IsExpired(now));
        if (!_generations.Any(x => x.IsActive(now)))
        {
            var generation = new Generation(now, this);
            _generations.Add(generation);

            return generation;
        }

        return _generations.Last();
    }

    public IReadOnlyList<Generation> CurrentGenerations => _generations;


    internal class Generation
    {
        public bool IsExpired(DateTimeOffset now)
        {
            return now > Expires;
        }

        public bool IsActive(DateTimeOffset now)
        {
            return now >= Start && now < End;
        }

        public DateTimeOffset Start { get; }
        public DateTimeOffset Expires { get; }

        public Generation(DateTimeOffset start, CircuitBreaker parent)
        {
            Start = start;
            Expires = start.Add(parent._options.TrackingPeriod);
            End = start.Add(parent.GenerationPeriod);
        }

        public int Failures { get; set; }
        public int Total { get; set; }
        public DateTimeOffset End { get; set; }

        public override string ToString()
        {
            return $"{nameof(Start)}: {Start}, {nameof(Expires)}: {Expires}, {nameof(End)}: {End}";
        }
    }

    public void Dispose()
    {
        _cancellation.Cancel();
        _processingBlock.Complete();
        _batching.Dispose();
    }
}
