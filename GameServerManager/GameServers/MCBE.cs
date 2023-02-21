﻿using System.Net;
using System.Text.RegularExpressions;
using GameServerManager.Attributes;
using GameServerManager.GameServers.Components;
using GameServerManager.GameServers.Configs;
using GameServerManager.GameServers.Protocols;
using GameServerManager.Utilities;
using ILogger = Serilog.ILogger;

namespace GameServerManager.GameServers
{
    public class MCBE : IGameServer, IVersionable
    {
        public class StartConfig : IStartConfig
        {
            [TextField(Label = "Start Path", HelperText = "Path to start the application.", Required = true)]
            public string StartPath { get; set; } = "bedrock_server.exe";

            [TextField(Label = "Start Parameter", HelperText = "Command-line arguments to use when starting the application.")]
            public string StartParameter { get; set; } = string.Empty;

            [RadioGroup(Text = "Console Type")]
            [Radio(Option = "Pseudo Console")]
            [Radio(Option = "Redirect")]
            [Radio(Option = "Windowed")]
            public string ConsoleMode { get; set; } = "Pseudo Console";
        }

        public class InstallConfig
        {
            [CheckBox(Label = "I agree to the [Minecraft End User License Agreement](https://minecraft.net/terms) and [Privacy Policy](https://go.microsoft.com/fwlink/?LinkId=521839)", Required = true, RequiredError = "You must agree")]
            public bool EndUserLicenseAgreement { get; set; }
        }

        public class Configuration : IConfig
        {
            public string LocalVersion { get; set; } = string.Empty;

            public string ClassName => nameof(MCBE);

            public Guid Guid { get; set; }

            [TabPanel(Text = "Basic", Description = "Basic Configuration")]
            public BasicConfig Basic { get; set; } = new();

            [TabPanel(Text = "Advanced", Description = "Advanced Configuration")]
            public AdvancedConfig Advanced { get; set; } = new();

            [TabPanel(Text = "Backup", Description = "Backup Configuration")]
            public BackupConfig Backup { get; set; } = new();

            [TabPanel(Text = "Start", Description = "Start Configuration")]
            public StartConfig Start { get; set; } = new();

            [TabPanel(Text = "Install", Description = "Install Configuration")]
            public InstallConfig Install { get; set; } = new();
        }

        public string Name => "Minecraft: Bedrock Edition Dedicated Server";

        public string ImageSource => $"/images/games/{nameof(MCBE)}.jpg";

        public IQueryProtocol? Protocol => null;

        public IQueryResponse? Response { get; set; }

        public ILogger Logger { get; set; } = default!;

        public IConfig Config { get; set; } = new Configuration();

        public Status Status { get; set; }

        public ProcessEx Process { get; set; } = new();

        public Task Install(string version) => Download(version, Config.Basic.Directory);

        public async Task Update(string version)
        {
            string temporaryDirectory = DirectoryEx.CreateTemporaryDirectory();

            await Download(version, temporaryDirectory);

            string[] excludedFiles =
            {
                "allowlist.json",
                "server.properties",
                "permissions.json",
                "whitelist.json",
            };

            foreach (string file in excludedFiles)
            {
                string path = Path.Combine(temporaryDirectory, file);
                await FileEx.DeleteIfExistsAsync(path);
            }

            await DirectoryEx.MoveAsync(temporaryDirectory, Config.Basic.Directory, true);
        }

        public Task Start()
        {
            Configuration config = (Configuration)Config;

            if (config.Start.ConsoleMode == "Pseudo Console")
            {
                Process.UsePseudoConsole(new()
                {
                    WorkingDirectory = config.Basic.Directory,
                    FileName = Path.Combine(config.Basic.Directory, config.Start.StartPath),
                    Arguments = config.Start.StartParameter,
                });
            }
            else if (config.Start.ConsoleMode == "Redirect")
            {
                Process.UseRedirect(new()
                {
                    WorkingDirectory = config.Basic.Directory,
                    FileName = Path.Combine(config.Basic.Directory, config.Start.StartPath),
                    Arguments = config.Start.StartParameter,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                });
            }
            else
            {
                Process.UseWindowed(new()
                {
                    WorkingDirectory = config.Basic.Directory,
                    FileName = Path.Combine(config.Basic.Directory, config.Start.StartPath),
                    Arguments = config.Start.StartParameter,
                });
            }

            return Process.Start();
        }

        public async Task Stop()
        {
            await Process.WriteLine("stop");

            bool exited = await Process.WaitForExit((int)TimeSpan.FromSeconds(10).TotalMilliseconds);

            if (!exited)
            {
                throw new Exception("Process fail to stop");
            }
        }
        
        public async Task<List<string>> GetVersions()
        {
            // Credit: https://github.com/WindowsGSM/WindowsGSM/pull/98
            // Thanks: https://github.com/BizakDaTroll
            using HttpClient httpClient = new(new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            });
            httpClient.DefaultRequestHeaders.Add("Accept", "*/*");

            using HttpResponseMessage response = await httpClient.GetAsync("https://www.minecraft.net/en-us/download/server/bedrock/");
            response.EnsureSuccessStatusCode();

            string content = await response.Content.ReadAsStringAsync();
            Regex regex = new(@"https:\/\/minecraft\.azureedge\.net\/bin-win\/(bedrock-server-(.*?)\.zip)");
            MatchCollection matches = regex.Matches(content);

            if (matches.Count <= 0)
            {
                throw new Exception("Could not find the download URL");
            }

            // Extract useful data from regex
            string version = matches[0].Groups[2].Value; // 1.14.21.0

            return new() { version };
        }

        private static async Task Download(string version, string directory)
        {
            using HttpClient httpClient = new(new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            });
            httpClient.DefaultRequestHeaders.Add("Accept", "*/*");

            // Download zip
            string fileName = $"bedrock-server-{version}.zip";
            string zipPath = Path.Combine(directory, fileName);
            using HttpResponseMessage response = await httpClient.GetAsync($"https://minecraft.azureedge.net/bin-win/{fileName}");
            response.EnsureSuccessStatusCode();

            using (FileStream fs = new(zipPath, FileMode.CreateNew))
            {
                await response.Content.CopyToAsync(fs);
            }

            // Extract then delete zip
            await FileEx.ExtractZip(zipPath, directory);
            await FileEx.DeleteAsync(zipPath);
        }
    }
}
