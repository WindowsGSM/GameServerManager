using System.Reflection.Emit;
using System.Text.RegularExpressions;
using GameServerManager.Attributes;
using GameServerManager.GameServers.Components;
using GameServerManager.GameServers.Configs;
using GameServerManager.Utilities;

namespace GameServerManager.GameServers.Mods
{
    public interface ISourceModConfig
    {
        public SourceMod.Config SourceMod { get; set; }
    }

    public class SourceMod : IMod
    {
        public string Name => nameof(SourceMod);

        public string Description => "SourceMod (SM) is an HL2 mod which allows you to write modifications for Half-Life 2 with the Small scripting language.";

        public Type ConfigType => typeof(ISourceModConfig);

        public class InstallConfig
        {
            [TextField(Label = "Install Path", HelperText = "SourceMod install directory", Required = true, FolderBrowser = true)]
            public string Path { get; set; } = "addons/sourcemod";
        }

        public class Config : IModConfig
        {
            public string LocalVersion { get; set; } = string.Empty;

            // For future fork server
            // [TabPanel(Text = "Install", Description = "Install Configuration")]
            public InstallConfig Install { get; set; } = new();
        }

        public IModConfig GetModConfig(IGameServer gameServer) => ((ISourceModConfig)gameServer.Config).SourceMod;

        public async Task<List<string>> GetVersions()
        {
            using HttpClient httpClient = new();
            using HttpResponseMessage response = await httpClient.GetAsync("https://sm.alliedmods.net/smdrop/");
            response.EnsureSuccessStatusCode();

            string content = await response.Content.ReadAsStringAsync();
            Regex regex = new("href=\"(\\d+\\.\\d+)");
            MatchCollection matches = regex.Matches(content); // Match href="1.10
            List<Version> versions = matches.Select(x => new Version(x.Groups[1].Value)).ToList(); // Values [1.10, 1.11, 1.7, 1.8, 1.9]
            versions.Sort();
            versions.Reverse();

            using HttpResponseMessage response2 = await httpClient.GetAsync("https://www.sourcemod.net/downloads.php?branch=stable");
            response2.EnsureSuccessStatusCode();

            content = await response2.Content.ReadAsStringAsync();
            regex = new("\\?branch=(\\d+\\.\\d+)");
            matches = regex.Matches(content); // Match ?branch=1.10
            string stableVersion = matches.Last().Groups[1].Value; // Value 1.10

            return new List<string> { stableVersion }.Concat(versions.Select(x => x.ToString())).Distinct().ToList();
        }

        public async Task Install(IGameServer gameServer, string version)
        {
            string modFolder = ((ISteamCMDConfig)gameServer.Config).SteamCMD.Game;
            string temporaryDirectory = await DownloadAndExtractZip(version, Path.Combine(gameServer.Config.Basic.Directory, modFolder));

            // Delete temporary directory
            await DirectoryEx.DeleteAsync(temporaryDirectory, true);

            // Update version
            ((ISourceModConfig)gameServer.Config).SourceMod.LocalVersion = version;
            await gameServer.Config.Update();
        }

        public async Task Update(IGameServer gameServer, string version)
        {
            string temporaryDirectory = await DownloadAndExtractZip(version);

            // Upgrade https://wiki.alliedmods.net/Upgrading_sourcemod
            string modFolder = ((ISteamCMDConfig)gameServer.Config).SteamCMD.Game;
            string installPath = ((ISourceModConfig)gameServer.Config).SourceMod.Install.Path;
            string newPath = Path.Combine(temporaryDirectory, installPath);
            string oldPath = Path.Combine(gameServer.Config.Basic.Directory, modFolder, installPath);
            string[] folders = { "bin", "extensions", "gamedata", "plugins", "translations" };

            // Overwrite the folders
            foreach (string folder in folders)
            {
                await DirectoryEx.MoveAsync(Path.Combine(newPath, folder), Path.Combine(oldPath, folder), true);
            }

            // Delete temporary directory
            await DirectoryEx.DeleteAsync(temporaryDirectory, true);

            // Update version
            ((ISourceModConfig)gameServer.Config).SourceMod.LocalVersion = version;
            await gameServer.Config.Update();
        }

        public async Task Delete(IGameServer gameServer)
        {
            string modFolder = ((ISteamCMDConfig)gameServer.Config).SteamCMD.Game;
            string modPath = Path.Combine(gameServer.Config.Basic.Directory, modFolder);
            string installPath = ((ISourceModConfig)gameServer.Config).SourceMod.Install.Path;

            // Delete folders and files
            await DirectoryEx.DeleteIfExistsAsync(Path.Combine(modPath, installPath), true);
            // await FileEx.DeleteIfExistsAsync(Path.Combine(modPath, "addons", "metamod", "sourcemod.vdf"));
            // await DirectoryEx.DeleteIfExistsAsync(Path.Combine(modPath, "cfg", "sourcemod"), true);

            // Update version
            ((ISourceModConfig)gameServer.Config).SourceMod.LocalVersion = string.Empty;
            await gameServer.Config.Update();
        }

        /// <summary>
        /// Download and Extract Zip
        /// </summary>
        /// <param name="version"></param>
        /// <param name="extractPath"></param>
        /// <returns>Temporary directory</returns>
        private static async Task<string> DownloadAndExtractZip(string version, string? extractPath = null)
        {
            using HttpClient httpClient = new();

            // Get latest windows sourcemod version file name
            using HttpResponseMessage response = await httpClient.GetAsync($"https://sm.alliedmods.net/smdrop/{version}/sourcemod-latest-windows");
            response.EnsureSuccessStatusCode();
            string latest = await response.Content.ReadAsStringAsync();

            // Download zip
            using HttpResponseMessage response2 = await httpClient.GetAsync($"https://sm.alliedmods.net/smdrop/{version}/{latest}");
            response2.EnsureSuccessStatusCode();
            string temporaryDirectory = DirectoryEx.CreateTemporaryDirectory();
            string zipPath = Path.Combine(temporaryDirectory, latest);

            using (FileStream fs = new(zipPath, FileMode.CreateNew))
            {
                await response2.Content.CopyToAsync(fs);
            }

            // Extract zip
            await FileEx.ExtractZip(zipPath, extractPath ?? temporaryDirectory, true);

            return temporaryDirectory;
        }
    }
}
