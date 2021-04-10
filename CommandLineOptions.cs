using CommandLine;

namespace minecraft_windows_service_wrapper
{
    public class CommandLineOptions
    {
        [Option(shortName: 'w', longName: "world-directory", Required = true, HelpText = "Minecraft world directory path")]
        public string WorldDirectory { get; set; }

        [Option(shortName: 'j', longName: "jar", Required = false, HelpText = "Minecraft server jar file path")]
        public string ServerJar { get; set; }
    }
}