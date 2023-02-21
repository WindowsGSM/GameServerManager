using GameServerManager.Attributes;
using GameServerManager.GameServers.Components;
using GameServerManager.GameServers.Configs;
using GameServerManager.GameServers.Protocols;
using GameServerManager.Utilities;
using ILogger = Serilog.ILogger;

namespace GameServerManager.GameServers
{
    public class DayZ : IGameServer
    {
        public class StartConfig : IStartConfig
        {
            [TextField(Label = "Start Path", Required = true)]
            public string StartPath { get; set; } = "DayZServer_x64.exe";

            [TextField(Label = "Start Parameter")]
            public string StartParameter { get; set; } = "-config=serverDZ.cfg -port=2302 -profiles=profiles -doLogs -adminLog -netLog";

            [RadioGroup(Text = "Console Mode")]
            [Radio(Option = "Redirect")]
            [Radio(Option = "Windowed")]
            public string ConsoleMode { get; set; } = "Redirect";
        }

        public class Configuration : IConfig, ISteamCMDConfig, IProtocolConfig
        {
            public string LocalVersion { get; set; } = string.Empty;

            public string ClassName { get; init; } = nameof(DayZ);

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
                }
            };

            [TabPanel(Text = "Start", Description = "Start Configuration")]
            public StartConfig Start { get; set; } = new();

            [TabPanel(Text = "SteamCMD", Description = "SteamCMD Configuration")]
            public SteamCMDConfig SteamCMD { get; set; } = new()
            {
                AppId = "223350"
            };

            [TabPanel(Text = "Protocol", Description = "Protocol Configuration")]
            public ProtocolConfig Protocol { get; set; } = new()
            {
                QueryPort = 27015
            };
        }

        public string Name => "DayZ Dedicated Server";

        public string ImageSource => $"/images/games/{nameof(DayZ)}.jpg";

        public IQueryProtocol? Protocol => null;

        public IQueryResponse? Response { get; set; }

        public ILogger Logger { get; set; } = default!;

        public IConfig Config { get; set; } = new Configuration();

        public Status Status { get; set; }

        public ProcessEx Process { get; set; } = new();

        public async Task Install(string version)
        {
            await SteamCMD.Start(this, version);

            string path = Path.Combine(Config.Basic.Directory, "serverDZ.cfg");
            string text = await File.ReadAllTextAsync(path);

            // Replace "EXAMPLE NAME" to name
            text = text.Replace("EXAMPLE NAME", Config.Basic.Name);

            await File.WriteAllTextAsync(path, text);
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

        public Task Stop()
        {
            Process.Kill();

            return Task.CompletedTask;
        }
    }
}
