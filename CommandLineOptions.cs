using System;
using CommandLine;

namespace minecraft_windows_service_wrapper
{
    public class CommandLineOptions
    {
        [Value(index: 0, Required = true, HelpText = "The path to the Minecraft server directory that contains the world directory, the server JAR, etc.")]
        public string ServerDirectory { get; set; }

        [Option(shortName: 'v', longName: "minecraft-version", Required = true, HelpText = "The version of Minecraft")]
        public Version MinecraftVersion { get; set; }

        [Option(shortName: 'p', longName: "port", Required = false, HelpText = "The port to run the Minecraft server on", Default = -1)]
        public int Port { get; set; }

        [Option(shortName: 'h', longName: "java-home", Required = false, HelpText = "The JAVA_HOME to use")]
        public string JavaHome { get; set; }

        [Option(shortName: 'j', longName: "jar-file", Required = false, HelpText = "The name of the JAR file to use in the server directory. Default: server.jar", Default = "server.jar")]
        public string JarFileName { get; set; }
    }
}