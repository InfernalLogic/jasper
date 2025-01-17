using BaselineTypeDiscovery;
using System;
using System.Threading.Tasks;
using Jasper;
using Microsoft.Extensions.Hosting;
using Oakton;
using Oakton.Resources;

[assembly: IgnoreAssembly]

namespace TestingSupport
{
    /// <summary>
    ///     Shortcut to bootstrap simple Jasper applications.
    ///     Syntactical sugar over Host.CreateDefaultBuilder().UseJasper().RunOaktonCommands(args);
    /// </summary>
    public static class JasperHost
    {
        /// <summary>
        ///     Creates a Jasper application for the current executing assembly
        ///     using all the default Jasper configurations
        /// </summary>
        /// <returns></returns>
        public static IHost Basic()
        {
            return bootstrap(new JasperOptions());
        }

        /// <summary>
        ///     Builds and initializes a IHost for the options
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        public static IHost For(JasperOptions options)
        {
            return bootstrap(options);
        }

        /// <summary>
        ///     Builds and initializes a IHost for the configured JasperOptions
        /// </summary>
        /// <param name="configure"></param>
        /// <returns></returns>
        public static IHost For(Action<JasperOptions> configure)
        {
            var registry = new JasperOptions();
            configure(registry);
            return bootstrap(registry);
        }

        private static IHost bootstrap(JasperOptions options)
        {
            return Host.CreateDefaultBuilder()
                .UseJasper(options, (c,o) => {})
                .UseResourceSetupOnStartup(StartupAction.ResetState)
                //.ConfigureLogging(x => x.ClearProviders())
                .Start();
        }


        /// <summary>
        ///     Shortcut to create a new empty WebHostBuilder with Jasper's default
        ///     settings, add Jasper with the supplied configuration, and bootstrap the application
        ///     from the command line
        /// </summary>
        /// <param name="args"></param>
        /// <param name="configure"></param>
        /// <returns></returns>
        public static Task<int> Run(string[] args, Action<HostBuilderContext, JasperOptions> configure)
        {
            return Host.CreateDefaultBuilder().UseJasper(configure).RunOaktonCommands(args);
        }
    }
}
