using System.Diagnostics;
using Baseline.Dates;
using IntegrationTests;
using Jasper;
using Jasper.Persistence.Durability;
using Jasper.Persistence.Marten;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Oakton.Resources;
using ScheduledJobTests.SqlServer;
using Shouldly;

namespace ScheduledJobTests.Postgresql;

public class marten_scheduled_jobs : IAsyncLifetime
{
    private IHost theHost;
    private readonly ScheduledMessageReceiver theReceiver = new();

    public async Task InitializeAsync()
    {
        theHost = await Host
            .CreateDefaultBuilder()
            .UseJasper(opts =>
            {
                opts.Advanced.ScheduledJobPollingTime = 50.Milliseconds();

                opts.Services.AddSingleton(theReceiver);

                opts.Publish(x => x.MessagesFromAssemblyContaining<ScheduledMessageReceiver>()
                    .ToLocalQueue("incoming").UseDurableInbox());

                opts.Handlers.Discovery(x =>
                {
                    x.DisableConventionalDiscovery();
                    x.IncludeType<ScheduledMessageCatcher>();
                });

                opts.Services.AddMarten(Servers.PostgresConnectionString)
                    .IntegrateWithJasper();
            })
            .StartAsync();

        await theHost.ResetResourceState();
    }

    public Task DisposeAsync()
    {
        return theHost.StopAsync();
    }

    protected Task ScheduleMessage(int id, int seconds)
    {
        return theHost.Services.GetRequiredService<IMessageContext>()
            .ScheduleAsync(new ScheduledMessage { Id = id }, seconds.Seconds());
    }

    protected Task ScheduleSendMessage(int id, int seconds)
    {
        return theHost.Services.GetRequiredService<IMessageContext>()
            .ScheduleAsync(new ScheduledMessage { Id = id }, seconds.Seconds());
    }

    protected int ReceivedMessageCount()
    {
        return theReceiver.ReceivedMessages.Count;
    }

    protected Task AfterReceivingMessages()
    {
        return theReceiver.Received;
    }

    protected int TheIdOfTheOnlyReceivedMessageShouldBe()
    {
        return theReceiver.ReceivedMessages.Single().Id;
    }

    protected async Task<int> PersistedScheduledCount()
    {
        var counts = await theHost.Services.GetRequiredService<IEnvelopePersistence>().Admin.FetchCountsAsync();
        return counts.Scheduled;
    }

    protected async Task PersistedScheduledCountShouldBe(int expected)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        var count = await PersistedScheduledCount();
        while (stopwatch.Elapsed < 5.Seconds() && count != expected)
        {
            await Task.Delay(100.Milliseconds());
            count = await PersistedScheduledCount();
        }

        count.ShouldBe(expected);
    }

    [Fact]
    public async Task execute_scheduled_job()
    {
        await ScheduleSendMessage(1, 7200);
        await ScheduleSendMessage(2, 5);
        await ScheduleSendMessage(3, 7200);

        ReceivedMessageCount().ShouldBe(0);

        await AfterReceivingMessages();



        TheIdOfTheOnlyReceivedMessageShouldBe().ShouldBe(2);

        while (await PersistedScheduledCount() != 2)
        {
            await Task.Delay(250.Milliseconds());
        }

        (await PersistedScheduledCount()).ShouldBe(2);
    }


}

public class ScheduledMessageReceiver
{
    public readonly IList<ScheduledMessage> ReceivedMessages = new List<ScheduledMessage>();

    public readonly TaskCompletionSource<ScheduledMessage> Source = new();

    public Task<ScheduledMessage> Received => Source.Task;
}
