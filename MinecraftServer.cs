using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace windows_service_wrapper
{
    public class MinecraftServer : BackgroundService
    {
        private readonly ILogger<MinecraftServer> logger;
        private readonly CommandLineOptions opts;
        private Process process;
        private Task outReader;
        private Task errReader;

        public MinecraftServer(ILogger<MinecraftServer> logger, CommandLineOptions opts)
        {
            this.logger = logger;
            this.opts = opts;
        }

        private Task StartReader(StreamReader reader, bool isError = false) => Task.Run(async () =>
        {
            while (true)
            {
                var line = await reader.ReadLineAsync();
                if (line == null)
                    return;
                if (isError)
                    logger.LogError(line);
                else
                    logger.LogInformation(line);
            }
        });

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("Minecraft world directory: " + opts.WorldDirectory);
            if (!Directory.Exists(opts.WorldDirectory))
                throw new Exception("Minecraft world directory not found");
            var serverJar = opts.ServerJar ?? Path.Combine(opts.WorldDirectory, "server.jar");
            logger.LogInformation("Server jar file: " + serverJar);
            if (!File.Exists(serverJar))
                throw new Exception("Server jar file not found");
            var processStartInfo = new ProcessStartInfo("java.exe")
            {
                ArgumentList = { "-Xmx8096M", "-Xms4096M", "-jar", serverJar, "--nogui" },
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = opts.WorldDirectory
            };
            process = Process.Start(processStartInfo);
            outReader = StartReader(process.StandardOutput);
            errReader = StartReader(process.StandardError, isError: true);
            if (process == null)
                throw new Exception("Process could not be started");

            while (!stoppingToken.IsCancellationRequested)
                await Task.Delay(1000, stoppingToken);

        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Issuing save-all");
            await process.StandardInput.WriteLineAsync("save-all");
            logger.LogInformation("Issuing stop");
            await process.StandardInput.WriteLineAsync("stop");
            logger.LogInformation("Waiting for java.exe to exit...");
            await process.WaitForExitAsync();
            await Task.WhenAll(outReader, errReader);

            await base.StopAsync(cancellationToken);
        }
    }
}