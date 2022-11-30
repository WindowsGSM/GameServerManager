﻿using System.Text;
using GameServerManager.Attributes;
using GameServerManager.GameServers.Components;
using GameServerManager.GameServers.Configs;
using GameServerManager.GameServers.Protocols;
using GameServerManager.Utilities;
using ILogger = Serilog.ILogger;

namespace GameServerManager.GameServers.Engines
{
    public abstract class SourceEngine : IGameServer
    {
        public class StartConfig : IStartConfig
        {
            [TextField(Label = "Start Path", HelperText = "Path to start the application.", Required = true)]
            public string StartPath { get; set; } = "srcds.exe";

            [TextField(Label = "Start Parameter", HelperText = "Command-line arguments to use when starting the application.")]
            public string StartParameter { get; set; } = string.Empty;

            [RadioGroup(Text = "Console Mode")]
            [Radio(Option = "Redirect")]
            [Radio(Option = "Windowed")]
            public string ConsoleMode { get; set; } = "Windowed";
        }

        public class Configuration : IConfig, ISteamCMDConfig, IProtocolConfig, IMetaModConfig, ISourceModConfig
        {
            public string LocalVersion { get; set; } = string.Empty;

            public string ClassName { get; init; } = string.Empty;

            public Guid Guid { get; set; }

            [TabPanel(Text = "Basic", Description = "Basic Configuration")]
            public BasicConfig Basic { get; set; } = new();

            [TabPanel(Text = "Advanced", Description = "Advanced Configuration")]
            public AdvancedConfig Advanced { get; set; } = new();

            [TabPanel(Text = "Backup", Description = "Backup Configuration")]
            public BackupConfig Backup { get; set; } = new();

            [TabPanel(Text = "Start", Description = "Start Configuration")]
            public StartConfig Start { get; set; } = new();

            [TabPanel(Text = "SteamCMD", Description = "SteamCMD Configuration")]
            public SteamCMDConfig SteamCMD { get; set; } = new();

            [TabPanel(Text = "Protocol", Description = "Protocol Configuration")]
            public ProtocolConfig Protocol { get; set; } = new()
            {
                QueryPort = 27015
            };

            public string MetaModLocalVersion { get; set; } = string.Empty;

            public string SourceModLocalVersion { get; set; } = string.Empty;
        }

        public virtual string Name => string.Empty;

        public virtual string ImageSource => string.Empty;

        public virtual IProtocol? Protocol => new SourceProtocol();

        public virtual ILogger Logger { get; set; } = default!;

        public virtual IConfig Config { get; set; } = new Configuration();

        public Status Status { get; set; }

        public ProcessEx Process { get; set; } = new();

        public Task<bool> Detect(string path)
        {
            return TaskEx.Run(() =>
            {
                return Directory.Exists(Path.Combine(path, ((ISteamCMDConfig)Config).SteamCMD.Game)) &&
                File.Exists(Path.Combine(path, "srcds.exe")) &&
                File.Exists(Path.Combine(path, "steam_appid.txt")) &&
                File.ReadAllText(Path.Combine(path, "steam_appid.txt")).Equals(((ISteamCMDConfig)Config).SteamCMD.AppId);
            });
        }

        public virtual Task<List<string>> GetVersions() => SteamCMD.GetVersions(this);

        public virtual Task Install(string version) => SteamCMD.Start(this, version);

        public virtual Task Update(string version) => SteamCMD.Start(this, version);

        public virtual Task Start()
        {
            Configuration config = (Configuration)Config;

            if (config.Start.ConsoleMode == "Redirect")
            {
                Process.UseRedirect(new()
                {
                    WorkingDirectory = config.Basic.Directory,
                    FileName = Path.Combine(config.Basic.Directory, config.Start.StartPath),
                    Arguments = config.Start.StartParameter,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                });
            }
            else
            {
                Process.UseWindowed(new()
                {
                    WorkingDirectory = config.Basic.Directory,
                    FileName = Path.Combine(config.Basic.Directory, config.Start.StartPath),
                    Arguments = config.Start.StartParameter
                });
            }

            return Process.Start();
        }

        public virtual async Task Stop()
        {
            await Process.WriteLine("quit");

            bool exited = await Process.WaitForExit((int)TimeSpan.FromSeconds(10).TotalMilliseconds);

            if (!exited)
            {
                throw new Exception("Process fail to stop");
            }
        }
    }
}
