using GameServerManager.Attributes;
using GameServerManager.GameServers.Components;
using GameServerManager.GameServers.Configs;
using GameServerManager.Shared;
using GameServerManager.Utilities;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using XtermBlazor;

namespace GameServerManager.Services
{
    public class ProgramDataService
    {
        /// <summary>
        /// ProgramData Path (All user) - Always C:\ProgramData\WindowsGSM
        /// </summary>
        public static readonly string ProgramDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "WindowsGSM");

        /// <summary>
        /// PathRoot Path (All user) - C:\
        /// </summary>
        public static readonly string PathRootPath = Path.GetPathRoot(ProgramDataPath)!;

        public static class Cache
        {
            /// <summary>
            /// Cache Path - Always C:\ProgramData\WindowsGSM\Cache
            /// </summary>
            public static readonly string Path = System.IO.Path.Combine(ProgramDataPath, "Cache");

            public static readonly JsonFile<string> InstallLastSelect = new(nameof(InstallLastSelect), Path);
            public static readonly JsonFile<string> ImportLastSelect = new(nameof(ImportLastSelect), Path);
            public static readonly JsonFile<MainLayout.Theme> Theme = new(nameof(Theme), Path);
        }

        public static class Data
        {
            /// <summary>
            /// Data Path - Always C:\ProgramData\WindowsGSM\Data
            /// </summary>
            public static readonly string Path = System.IO.Path.Combine(ProgramDataPath, "Data");

            public static readonly JsonFile<List<string>> ServersOrder = new(nameof(ServersOrder), Path);
        }

        /// <summary>
        /// Game Servers Configs
        /// </summary>
        public static class Configs
        {
            /// <summary>
            /// Game Servers Configs Path - Always C:\ProgramData\WindowsGSM\Configs
            /// </summary>
            public static readonly string Path = System.IO.Path.Combine(ProgramDataPath, "Configs");

            /// <summary>
            /// Get game server configs path
            /// </summary>
            /// <param name="guid"></param>
            /// <returns>Return C:\ProgramData\WindowsGSM\Configs\{Guid}.json</returns>
            public static string GetPath(Guid guid)
            {
                Directory.CreateDirectory(Path);

                return System.IO.Path.Combine(Path, $"{guid}.json");
            }

            public static List<string> GetFiles()
            {
                Directory.CreateDirectory(Path);

                return Directory.GetFiles(Path, "*.json", SearchOption.TopDirectoryOnly).ToList();
            }

            public static bool TryGetGameServer(string guid, [NotNullWhen(true)] out IGameServer? server)
            {
                try
                {
                    string path = GetPath(Guid.Parse(guid));
                    string json = File.ReadAllText(path);
                    server = Deserialize(json);

                    return true;
                }
                catch
                {
                    server = null;

                    return false;
                }
            }

            private static IGameServer Deserialize(string json)
            {
                Dictionary<string, object> config = JsonSerializer.Deserialize<Dictionary<string, object>>(json)!;
                IGameServer gameServer = (IGameServer)Activator.CreateInstance(Type.GetType($"{nameof(GameServerManager)}.GameServers.{config["ClassName"]}")!)!;
                gameServer.Config = (IConfig)JsonSerializer.Deserialize(json, gameServer.Config.GetType())!;

                return gameServer;
            }
        }

        public static class Logs
        {
            /// <summary>
            /// Game Servers Logs Path - Always C:\ProgramData\WindowsGSM\Logs
            /// </summary>
            public static readonly string Path = System.IO.Path.Combine(ProgramDataPath, "Logs");

            /// <summary>
            /// Get game server logs path
            /// </summary>
            /// <param name="guid"></param>
            /// <returns>Return C:\ProgramData\WindowsGSM\Logs\{guid}\</returns>
            public static string GetPath(Guid guid)
            {
                Directory.CreateDirectory(Path);

                return System.IO.Path.Combine(Path, guid.ToString());
            }

            /// <summary>
            /// Get game server logs file
            /// </summary>
            /// <param name="guid"></param>
            /// <param name="fileName"></param>
            /// <returns>Return C:\ProgramData\WindowsGSM\Logs\{guid}\{fileName}</returns>
            public static string GetFile(Guid guid, string fileName)
            {
                return System.IO.Path.Combine(GetPath(guid), fileName);
            }

            /// <summary>
            /// Get game server logs files
            /// </summary>
            /// <param name="guid"></param>
            /// <returns>Return C:\ProgramData\WindowsGSM\Logs\{guid}\{fileName}</returns>
            public static Task<List<string>> GetFiles(Guid guid)
            {
                DirectoryInfo info = new(GetPath(guid));

                return Task.Run(() => info.GetFiles("*.txt", SearchOption.TopDirectoryOnly).OrderByDescending(p => p.CreationTime).Select(x => x.Name).ToList());
            }
        }

        /// <summary>
        /// Servers Path - Always C:\GameServers
        /// </summary>
        public class Servers
        {
            public static readonly string ServersPath = Path.Combine(PathRootPath, "GameServers");

            public static void Create()
            {

            }
        }





        /// <summary>
        /// WindowsGSM Global Settings
        /// </summary>
        public class Settings
        { 
            [TextField(Label = "Backups Path", HelperText = "Backups Path", Required = true)]
            public string BackupsPath { get; set; } = Path.Combine(ProgramDataPath, "Backups");

            [TextField(Label = "Servers Path", HelperText = "Servers Path", Required = true)]
            public string ServersPath { get; set; } = Path.Combine(PathRootPath, "GameServers");
        }

        private static readonly Settings settingsInstance = new();

        public static void UpdateSettings(Settings settings)
        {
            
        }
    }
}
