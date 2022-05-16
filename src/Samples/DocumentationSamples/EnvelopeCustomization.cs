using Baseline.Dates;
using Jasper;
using Jasper.Transports.Tcp;
using Microsoft.Extensions.Hosting;

namespace DocumentationSamples
{
    public static class EnvelopeCustomizationSamples
    {
        public static async Task monitoring_data_publisher()
        {
            #region sample_MonitoringDataPublisher

            using var host = Host.CreateDefaultBuilder()
                .UseJasper(opts =>
                {
                    opts.PublishAllMessages()
                        .ToPort(2222)

                        // Set a message expiration on all
                        // outgoing messages to this
                        // endpoint
                        .CustomizeOutgoing(env => env.DeliverWithin = 2.Seconds());
                }).StartAsync();

            #endregion
        }
    }

}
