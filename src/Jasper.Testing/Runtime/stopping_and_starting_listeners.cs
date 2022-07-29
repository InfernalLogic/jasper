using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Baseline.Dates;
using Jasper.ErrorHandling;
using Jasper.Testing.Transports.Tcp;
using Jasper.Tracking;
using Jasper.Transports;
using Jasper.Transports.Tcp;
using Jasper.Util;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shouldly;
using TestingSupport;
using Xunit;

namespace Jasper.Testing.Runtime
{
    public class stopping_and_starting_listeners : IDisposable
    {
        private readonly IHost theListener;
        private readonly int _port1;
        private readonly int _port2;
        private readonly int _port3;

        public stopping_and_starting_listeners()
        {
            _port1 = PortFinder.GetAvailablePort();
            _port2 = PortFinder.GetAvailablePort();
            _port3 = PortFinder.GetAvailablePort();

            theListener = JasperHost.For(opts =>
            {
                opts.ListenAtPort(_port1).Named("one");
                opts.ListenAtPort(_port2).Named("two");
                opts.ListenAtPort(_port3).Named("three");

                opts.Handlers.OnException<DivideByZeroException>()
                    .RequeueAndPauseProcessing(3.Seconds());
            });


        }

        [Fact]
        public void find_listener_by_name()
        {
            var runtime = theListener.GetRuntime();
            runtime.FindListeningAgent("one")
                .Uri.ShouldBe($"tcp://localhost:{_port1}".ToUri());

            runtime.FindListeningAgent("wrong")
                .ShouldBeNull();
        }

        [Fact]
        public void all_listeners_are_initially_listening()
        {
            var uri1 = $"tcp://localhost:{_port1}".ToUri();
            var uri2 = $"tcp://localhost:{_port2}".ToUri();
            var uri3 = $"tcp://localhost:{_port3}".ToUri();

            var runtime = theListener.GetRuntime();

            runtime.FindListeningAgent(uri1).Status.ShouldBe(ListeningStatus.Accepting);
            runtime.FindListeningAgent(uri2).Status.ShouldBe(ListeningStatus.Accepting);
            runtime.FindListeningAgent(uri3).Status.ShouldBe(ListeningStatus.Accepting);
        }

        [Fact]
        public void unknown_listener_is_unknown()
        {
            theListener.GetRuntime().FindListeningAgent("unknown://server".ToUri())
                .ShouldBeNull();
        }

        [Fact]
        public async Task stop_with_no_restart()
        {
            var agent = theListener.GetRuntime().FindListeningAgent("one");
            await agent.StopAsync();

            agent.Status.ShouldBe(ListeningStatus.Stopped);

            await agent.StartAsync();

            agent.Status.ShouldBe(ListeningStatus.Accepting);
        }

        [Fact]
        public async Task pause()
        {
            var agent = theListener.GetRuntime().FindListeningAgent("one");
            await agent.PauseAsync(3.Seconds());

            agent.Status.ShouldBe(ListeningStatus.Stopped);

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            while (stopwatch.Elapsed < 10.Seconds())
            {
                if (agent.Status == ListeningStatus.Accepting)
                {
                    stopwatch.Stop();
                    return;
                }
            }

            agent.Status.ShouldBe(ListeningStatus.Accepting);
        }

        [Fact]
        public async Task pause_repeatedly()
        {
            var agent = theListener.GetRuntime().FindListeningAgent("one");
            await agent.PauseAsync(1.Seconds());
            await agent.PauseAsync(1.Seconds());
            await agent.PauseAsync(3.Seconds());

            agent.Status.ShouldBe(ListeningStatus.Stopped);

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            while (stopwatch.Elapsed < 10.Seconds())
            {
                if (agent.Status == ListeningStatus.Accepting)
                {
                    stopwatch.Stop();
                    return;
                }
            }

            agent.Status.ShouldBe(ListeningStatus.Accepting);
        }

        [Fact]
        public async Task pause_listener_on_matching_error_condition()
        {
            var publisher = theListener.Services.GetRequiredService<IMessagePublisher>();
            await publisher.SendToEndpointAsync("one", new PausingMessage());

            var runtime = theListener.GetRuntime();
            var agent = runtime.FindListeningAgent("one");
            agent.Status.ShouldBe(ListeningStatus.Stopped);

            // should restart
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            while (stopwatch.Elapsed < 10.Seconds())
            {
                if (agent.Status == ListeningStatus.Accepting)
                {
                    stopwatch.Stop();
                    return;
                }
            }

            agent.Status.ShouldBe(ListeningStatus.Accepting);
        }

        public void Dispose()
        {
            theListener?.Dispose();
        }
    }

    public class PausingMessage{}

    public class PausingMessageHandler
    {
        public static void Handle(PausingMessage message, Envelope envelope)
        {
            if (envelope.Attempts <= 1)
            {
                throw new DivideByZeroException("boom");
            }
        }
    }

}
