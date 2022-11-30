using GameServerManager.Attributes;
using GameServerManager.GameServers.Components;
using GameServerManager.GameServers.Configs;
using GameServerManager.GameServers.Protocols;
using GameServerManager.Utilities;
using ILogger = Serilog.ILogger;

namespace GameServerManager.GameServers
{
    /// <summary>
    /// Subsistence Dedicated Server
    /// </summary>
    public class Subsistence : IGameServer
    {
        public class StartConfig : IStartConfig
        {
            [TextField(Label = "Start Path", HelperText = "Path to start the application.", Required = true)]
            public string StartPath { get; set; } = "Binaries\\Win64\\UDK.exe";

            [TextField(Label = "Start Parameter", HelperText = "Command-line arguments to use when starting the application.")]
            public string StartParameter { get; set; } = "server coldmap1?steamsockets -log";
        }

        public class Configuration : IConfig, ISteamCMDConfig, IProtocolConfig
        {
            public string LocalVersion { get; set; } = string.Empty;

            public string ClassName => nameof(Subsistence);

            public Guid Guid { get; set; }

            [TabPanel(Text = "Basic", Description = "Basic Configuration")]
            public BasicConfig Basic { get; set; } = new();

            [TabPanel(Text = "Advanced", Description = "Advanced Configuration")]
            public AdvancedConfig Advanced { get; set; } = new();

            [TabPanel(Text = "Backup", Description = "Backup Configuration")]
            public BackupConfig Backup { get; set; } = new()
            {
                Entries =
                {
                    "UDKGame"
                }
            };

            [TabPanel(Text = "Start", Description = "Start Configuration")]
            public StartConfig Start { get; set; } = new();

            [TabPanel(Text = "SteamCMD", Description = "SteamCMD Configuration")]
            public SteamCMDConfig SteamCMD { get; set; } = new()
            {
                AppId = "1362640",
                Username = "anonymous"
            };

            [TabPanel(Text = "Protocol", Description = "Protocol Configuration")]
            public ProtocolConfig Protocol { get; set; } = new()
            {
                QueryPort = 27015
            };
        }

        public string Name => "Subsistence Dedicated Server";

        public string ImageSource => $"/images/games/{nameof(Subsistence)}.jpg";

        public IProtocol? Protocol => new SourceProtocol();

        public ILogger Logger { get; set; } = default!;

        public IConfig Config { get; set; } = new Configuration();

        public Status Status { get; set; }

        public ProcessEx Process { get; set; } = new();

        public Task<List<string>> GetVersions() => SteamCMD.GetVersions(this);

        public async Task Install(string version)
        {
            await SteamCMD.Start(this, version);

            // Once downloaded, the app needs to be run once to generate the UDK*.ini files
            await Start();
            await Stop();
        }

        public Task Update(string version) => SteamCMD.Start(this, version);

        public Task Start()
        {
            Configuration config = (Configuration)Config;

            Process.UseWindowed(new()
            {
                WorkingDirectory = config.Basic.Directory,
                FileName = Path.Combine(config.Basic.Directory, config.Start.StartPath),
                Arguments = config.Start.StartParameter
            });

            return Process.Start();
        }

        public async Task Stop()
        {
            Process.Kill();

            bool exited = await Process.WaitForExit((int)TimeSpan.FromSeconds(10).TotalMilliseconds);

            if (!exited)
            {
                throw new Exception("Process fail to stop");
            }
        }
    }
}
