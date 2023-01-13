using Microsoft.Extensions.Hosting.WindowsServices;
using Serilog;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using GameServerManager.GameServers.Components;
using GameServerManager.GameServers.Configs;
using GameServerManager.GameServers.Mods;
using GameServerManager.GameServers.Protocols;
using GameServerManager.Utilities;
using static GameServerManager.GameServers.Components.SteamCMD;

namespace GameServerManager.Services
{
    public class GameServerService : IHostedService, IDisposable
    {
        public static readonly bool IsWindowsService = WindowsServiceHelpers.IsWindowsService();

        /// <summary>
        /// WindowsGSM.exe Path - Always C:\Program Files\WindowsGSM
        /// </summary>
        public static readonly string ProcessPath = Path.GetDirectoryName(Environment.ProcessPath)!;

        public static readonly string DefaultBackupsPath = Path.Combine(ProgramDataService.ProgramDataPath, "backups");

        public static event Action? GameServersHasChanged;
        public static void InvokeGameServersHasChanged() => GameServersHasChanged?.Invoke();

        public static List<IGameServer> Instances { get; private set; } = new();

        public static List<IGameServer> GameServers => Assembly.GetExecutingAssembly().GetTypes()
            .Where(x => x.GetInterfaces().Contains(typeof(IGameServer)) && !x.IsAbstract)
            .Select(x => (Activator.CreateInstance(x) as IGameServer)!).OrderBy(x => x.Name).ToList();

        public static List<IMod> Mods => Assembly.GetExecutingAssembly().GetTypes()
            .Where(x => x.GetInterfaces().Contains(typeof(IMod)) && !x.IsAbstract)
            .Select(x => (Activator.CreateInstance(x) as IMod)!).ToList();

        public interface IVersions
        {
            public List<string> Versions { get; set; }

            public DateTime DateTime { get; set; }

            public string LatestVersion
            {
                get
                {
                    return Versions.Count <= 0 ? string.Empty : Versions[0];
                }
            }
        }

        public class VersionData : IVersions
        {
            public List<string> Versions { get; set; } = new();

            public DateTime DateTime { get; set; }
        }

        public static ConcurrentDictionary<Type, IVersions> Versions { get; private set; } = new();
        public static ConcurrentDictionary<Guid, IResponse> Responses { get; private set; } = new();

        private readonly ILogger<GameServerService> _logger;
        private Timer? _versionsTimer, _protocolTimer;

        public GameServerService(ILogger<GameServerService> logger)
        {
            _logger = logger;

            InitializeInstances();
            AutoStartInstances();
        }

        private static async void AutoStartInstances()
        {
            await Parallel.ForEachAsync(Instances.Where(x => x.Status == Status.Stopped && x.Config.Advanced.AutoStart), async (gameServer, token) =>
            {
                try
                {
                    await gameServer.StartAsync();
                }
                catch
                {

                }
            });
        }

        private static void InitializeInstances()
        {
            if (ProgramDataService.Data.ServerGuids.TryRead(out List<string>? guids))
            {
                foreach (string guid in guids)
                {
                    if (ProgramDataService.Configs.TryGetGameServer(guid, out IGameServer? gameServer) && !Instances.Select(x => x.Config.Guid).Contains(gameServer.Config.Guid))
                    {
                        AddInstance(gameServer);
                    }
                }
            }

            foreach (string path in ProgramDataService.Configs.GetFiles())
            {
                if (ProgramDataService.Configs.TryGetGameServer(Path.GetFileName(path), out IGameServer? gameServer) && !Instances.Select(x => x.Config.Guid).Contains(gameServer.Config.Guid))
                {
                    AddInstance(gameServer);
                }
            }

            UpdateServerGuids();
        }

        public static void UpdateServerGuids()
        {
            ProgramDataService.Data.ServerGuids.Write(Instances.Select(x => x.Config.Guid.ToString()).ToList());
        }

        public static void AddInstance(IGameServer gameServer)
        {
            Instances.Add(gameServer);

            string path = ProgramDataService.Logs.GetPath(gameServer.Config.Guid);
            gameServer.Logger = new LoggerConfiguration().WriteTo.File(Path.Combine(path, "log.txt"), rollingInterval: RollingInterval.Day, rollOnFileSizeLimit: true).CreateLogger();
            gameServer.Status = string.IsNullOrEmpty(gameServer.Config.LocalVersion) ? Status.NotInstalled : Status.Stopped; 
            gameServer.Process.Exited += async (exitCode) => await OnGameServerExited(gameServer);

            GameServersHasChanged?.Invoke();
        }

        public static void RemoveInstance(IGameServer gameServer)
        {
            Instances.Remove(gameServer);
            GameServersHasChanged?.Invoke();
        }

        private static async Task OnGameServerExited(IGameServer gameServer)
        {
            if (gameServer.Status == Status.Restarting)
            {
                return;
            }

            if (gameServer.Status == Status.Started && gameServer.Config.Advanced.RestartOnCrash)
            {
                gameServer.UpdateStatus(Status.Restarting);

                Responses.Remove(gameServer.Config.Guid, out _);

                await Task.Delay(5000);
                await gameServer.StartAsync();
            }
            else
            {
                gameServer.UpdateStatus(Status.Stopped);
            }
        }

        /// <summary>
        /// Get new BasicConfig with name and directory initalized
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        public BasicConfig GetNewBasicConfig(Guid guid)
        { 
            string[] names = Instances.Select(x => x.Config.Basic.Name).ToArray();
            int number = Instances.Count;

            while (names.Contains($"WindowsGSM - Server #{++number}"));

            return new()
            {
                Name = $"WindowsGSM - Server #{number}",
                Directory = Path.Combine(ProgramDataService.Servers.ServersPath, guid.ToString()),
            };
        }

        public async Task Backup(IGameServer gameServer)
        {
            gameServer.UpdateStatus(Status.Backuping);

            try
            {
                await BackupRestore.PerformFullBackup(gameServer);
            }
            catch
            {
                gameServer.UpdateStatus(Status.Stopped);
                throw;
            }

            gameServer.UpdateStatus(Status.Stopped);
        }

        public async Task Restore(IGameServer gameServer, string fileName)
        {
            gameServer.UpdateStatus(Status.Restoring);

            try
            {
                await BackupRestore.PerformRestore(gameServer, fileName);
            }
            catch
            {
                gameServer.UpdateStatus(Status.Stopped);
                throw;
            }

            gameServer.UpdateStatus(Status.Stopped);
        }

        public IVersions? GetVersions(IVersionable versionable)
        {
            return Versions.GetValueOrDefault(versionable.GetType());
        }

        public IResponse? GetResponse(IGameServer gameServer)
        {
            return Responses.GetValueOrDefault(gameServer.Config.Guid);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _versionsTimer = new Timer(FetchLatestVersions, null, TimeSpan.Zero, TimeSpan.FromSeconds(60 * 5));
            _protocolTimer = new Timer(QueryGameServers, null, TimeSpan.Zero, TimeSpan.FromSeconds(10));

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _versionsTimer?.Change(Timeout.Infinite, 0);
            _protocolTimer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }
        
        private async void FetchLatestVersions(object? state)
        {
            List<IVersionable> versionables = new();
            versionables.AddRange(GameServers);
            versionables.AddRange(Mods);

            await Parallel.ForEachAsync(versionables, async (versionable, token) =>
            {
                Type type = versionable.GetType();

                try
                {
                    VersionData version = new()
                    {
                        Versions = await versionable.GetVersions(),
                        DateTime = DateTime.Now,
                    };

                    Versions[type] = version;

                    _logger.LogInformation($"FetchLatestVersions {version.Versions[0]} ({type.Name})");
                }
                catch (Exception e)
                {
                    _logger.LogError($"Fail to FetchLatestVersions ({type.Name}) {e}");
                }
            });

            List<(IGameServer, string)> items = Instances
                .Where(x => x.Config.Advanced.AutoUpdateAndRestart && x.Status == Status.Started && x.IsUpdateAvailable())
                .Select(x => (x, GetVersions(x)!.Versions[0]))
                .ToList();

            await Parallel.ForEachAsync(items, async (item, token) =>
            {
                IGameServer gameServer = item.Item1;
                string version = item.Item2;

                try
                {
                    gameServer.Logger.Information("Running Auto Update and Restart...");

                    gameServer.Logger.Information("Server stopping...");
                    gameServer.UpdateStatus(Status.Stopping);

                    try
                    {
                        await gameServer.Stop();

                        Responses.Remove(gameServer.Config.Guid, out _);
                    }
                    catch (Exception ex)
                    {
                        gameServer.Logger.Error(ex, "Server failed to stop");
                        gameServer.UpdateStatus(Status.Started);

                        throw;
                    }

                    gameServer.Logger.Information("Server stopped");

                    gameServer.Logger.Information("Server updating...");
                    gameServer.UpdateStatus(Status.Updating);

                    try
                    {
                        await gameServer.Update(version);

                        gameServer.Config.LocalVersion = version;
                        await gameServer.Config.Update();
                    }
                    catch (BuildIdMismatchException ex)
                    {
                        gameServer.Logger.Warning($"Server current version is {ex.Message} instead of {version}");

                        gameServer.Config.LocalVersion = ex.Message;
                        await gameServer.Config.Update();
                    }
                    catch (Exception ex)
                    {
                        gameServer.Logger.Error(ex, "Server failed to update");
                        gameServer.UpdateStatus(Status.Stopped);

                        throw;
                    }

                    gameServer.Logger.Information("Server updated");

                    await gameServer.StartAsync();

                    gameServer.Logger.Information("Auto Update and Restart has been run successfully");
                }
                catch
                {
                    gameServer.Logger.Error("Auto Update and Restart has encountered some issues");
                }
            });
        }

        private async void QueryGameServers(object? state)
        {
            List<IGameServer> queryList = Instances.Where(x => x.Protocol != null && x.Status == Status.Started).ToList();

            _logger.LogInformation($"Query Servers: Start {queryList.Count}");

            await Parallel.ForEachAsync(queryList, async (gameServer, token) =>
            {
                try
                {
                    Responses[gameServer.Config.Guid] = await gameServer.Protocol!.Query((IProtocolConfig)gameServer.Config);
                }
                catch
                {

                }
            });

            _logger.LogInformation($"Query Servers: Done");
        }
        
        /// <inheritdoc/>
        public void Dispose()
        {
            _versionsTimer?.Dispose();
            _protocolTimer?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
