using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace minecraft_windows_service_wrapper
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
            try
            {
                logger.LogInformation("Minecraft server directory: " + opts.ServerDirectory);
                if (!Directory.Exists(opts.ServerDirectory))
                    throw new Exception("Minecraft server directory not found");

                var javaHome = opts.JavaHome ?? Environment.GetEnvironmentVariable("JAVA_HOME");
                logger.LogInformation("JAVA_HOME: " + opts.JavaHome);

                var javaExe = Path.Combine(javaHome, "bin", "java.exe");
                var javaVersion = await GetJavaVersion(javaExe);
                logger.LogInformation("Java version: " + javaVersion);

                var processStartInfo = GetProcessStartInfo(javaHome, javaExe, javaVersion);

                process = Process.Start(processStartInfo);
                if (process == null)
                    throw new Exception("Process could not be started");
                outReader = StartReader(process.StandardOutput);
                errReader = StartReader(process.StandardError, isError: true);

                while (!stoppingToken.IsCancellationRequested)
                    await Task.Delay(1000, stoppingToken);
            }
            catch (Exception e) when (!(e is TaskCanceledException))
            {
                logger.LogError(e, "Error in service");
            }
        }

        private ProcessStartInfo GetProcessStartInfo(string javaHome, string javaExe, int javaVersion)
        {
            var serverJar = Path.Combine(opts.ServerDirectory, opts.JarFileName);
            logger.LogInformation("Server jar file: " + serverJar);
            if (!File.Exists(serverJar))
                throw new Exception("Server jar file not found");

            var processStartInfo = new ProcessStartInfo(javaExe)
            {
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = opts.ServerDirectory,
            };
            if (opts.JavaHome != null)
                processStartInfo.Environment.Add("JAVA_HOME", javaHome);
            var args = javaVersion switch
            {
                8 => GetJavaVersion8Args(opts, serverJar),
                15 => GetJavaVersion15Args(opts, serverJar),
                _ => throw new NotSupportedException("Java version: " + javaVersion)
            };
            foreach (var arg in args)
                processStartInfo.ArgumentList.Add(arg);
            logger.LogInformation("Command line: " + processStartInfo.FileName + " " + string.Join(" ", processStartInfo.ArgumentList));
            return processStartInfo;
        }

        private IEnumerable<string> GetJavaVersion8Args(CommandLineOptions opts, string serverJar)
        {
            yield return "-Xms3G";
            yield return "-Xmx8G";
            yield return "-XX:+UseG1GC";
            yield return "-XX:+UnlockExperimentalVMOptions";
            yield return "-XX:MaxGCPauseMillis=100";
            yield return "-XX:+DisableExplicitGC";
            yield return "-XX:TargetSurvivorRatio=90";
            yield return "-XX:G1NewSizePercent=50";
            yield return "-XX:G1MaxNewSizePercent=80";
            yield return "-XX:G1MixedGCLiveThresholdPercent=50";
            yield return "-XX:+AlwaysPreTouch";
            yield return "-jar";
            yield return serverJar;
            foreach (var arg in GetMinecraftArgs(opts))
                yield return arg;
        }

        private IEnumerable<string> GetJavaVersion15Args(CommandLineOptions opts, string serverJar)
        {
            yield return "-Xms2G";
            yield return "-Xmx8G";
            yield return "-jar";
            yield return serverJar;
            foreach (var arg in GetMinecraftArgs(opts))
                yield return arg;
        }

        private IEnumerable<string> GetMinecraftArgs(CommandLineOptions opts)
        {
            if (opts.MinecraftVersion.Minor == 12)
            {
                yield return "nogui";
                yield return "--port";
                yield return opts.Port.ToString();
            }
            else if (opts.MinecraftVersion.Minor >= 16)
            {
                yield return "--nogui";
                yield return "--port";
                yield return opts.Port.ToString();
            }
            else
                throw new NotSupportedException("Minecraft version: " + opts.MinecraftVersion);
        }

        private async Task<int> GetJavaVersion(string javaExe)
        {
            var process = Process.Start(new ProcessStartInfo(javaExe)
            {
                Arguments = "-version",
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden,
            });

            var result = await Task.WhenAll(process.StandardOutput.ReadToEndAsync(), process.StandardError.ReadToEndAsync());
            var version = new Version(Regex.Match(result[1], "[0-9]+\\.[0-9]+\\.[0-9]+").Value);

            if (version.Major == 1)
                return version.Minor;
            return version.Major;
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            if (process == null)
                return;

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