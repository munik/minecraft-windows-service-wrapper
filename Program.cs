using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.EventLog;
using Microsoft.Extensions.Logging;
using CommandLine;
using System.Threading.Tasks;

namespace windows_service_wrapper
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            return await Parser.Default.ParseArguments<CommandLineOptions>(args)
                .MapResult(async (opts) =>
                {
                    await CreateHostBuilder(args, opts).Build().RunAsync();
                    return 0;
                },
                errs => Task.FromResult(-1)); // Invalid arguments
        }

        public static IHostBuilder CreateHostBuilder(string[] args, CommandLineOptions opts) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureLogging(configureLogging => configureLogging.AddFilter<EventLogLoggerProvider>(level => level >= LogLevel.Information))
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton(opts);
                    services.AddHostedService<MinecraftServer>()
                        .Configure<EventLogSettings>(config =>
                        {
                            config.LogName = "Application";
                            config.SourceName = "Minecraft Windows Service Wrapper";
                        });
                }).UseWindowsService();
    }
}
