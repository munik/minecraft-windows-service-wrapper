Minecraft Windows Service Wrapper
===

A Windows service wrapper for Minecraft Java Edition.
Launches java.exe for a Minecraft server, relays standard output and standard error into .NET logging infra, and issues `save-all` and `stop` when service shuts down.

Tested support for Minecraft 1.16 and 1.12.
Possibly supports other versions but not tested.

# <a id="installation">Installation</a>

1. First, you'll need to build the service executable:
    - Download [.NET 5 SDK](https://dotnet.microsoft.com/download/dotnet/5.0)
    - Clone this repository
    - Run `dotnet build -c release` in the root of the repo
2. Next, you should test running the service as an executable in a console:
    - At minimum, you'll need to specify the Minecraft server directory (e.g. the directory that contains server.properties, server.jar, the world directory) and the Minecraft version, e.g.:  
        `minecraft-windows-service-wrapper.exe D:\minecraft\family-server -v 1.16`
    - If needed, you can also specify a specific port to run on, a specific Java home directory to use, and a different JAR file name than "server.jar", e.g.:  
        `minecraft-windows-service-wrapper.exe D:\minecraft\pixelmon -v 1.12 -p 25568 -h "C:\Program Files\Amazon Corretto\jdk1.8.0_282" -j forge-1.12.2-14.23.5.2854.jar`  
        (See below for full command line documentation under <a href="#usage">Usage</a>.)
3. Once you've tested that it runs correctly, you'll need to install the service into Windows:
    - Run the following command:  
        `sc create "Your Minecraft Windows Service Name" binpath="THE COMMAND LINE YOU USED ABOVE" start= auto`  
        (It looks like there's typo in that `start=` and `auto` are separated by a space, but that is in fact correct.)
        If you don't want it to autostart, just remove the `start= auto`
    - Start the service!

# Troubleshooting

If you haven't already, you should follow step 2 under <a href="#installation">Installation</a>.
The output will relay standard out and standard error to the console.
If there's anything wrong with your Minecraft setup, this should help immensely.

In addition, certain errors will get logged to the Windows event log.
Events are logged in the Application event log under the "Minecraft Windows Service Wrapper" event source.

# <a id="usage">Usage</a>

Here is the command line documentation shown by `--help`:
```
minecraft-windows-service-wrapper 1.0.0
Copyright (C) 2021 minecraft-windows-service-wrapper

-v, --minecraft-version    Required. The version of Minecraft, e.g. 1.12 or
                            1.16.5

-p, --port                 (Default: -1) Optional: The port to run the
                            Minecraft server on. If not specified, the default
                            Minecraft server port is used.

-h, --java-home            Optional: Specify a Java home directory to use. By
                            default, the JAVA_HOME environment variable is
                            used.

-j, --jar-file             (Default: server.jar) Optional: The name of the JAR
                            file to use in the server directory. By default,
                            the JAR file name is assumed to be server.jar.

--help                     Display this help screen.

--version                  Display version information.

value pos. 0               Required. The path to the Minecraft server
                            directory that contains the world directory, the
                            server JAR, server.properties, etc.
```
