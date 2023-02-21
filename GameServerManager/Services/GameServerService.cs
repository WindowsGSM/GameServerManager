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

        private readonly ILogger<GameServerService> _logger;
        private Timer? _protocolTimer;

        public GameServerService(ILogger<GameServerService> logger)
        {
            _logger = logger;

            InitializeInstances();
            AutoStartInstances();
        }

        private async void AutoStartInstances()
        {
            await FetchBranches();

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
            if (ProgramDataService.Data.ServersOrder.TryRead(out List<string>? guids))
            {
                foreach (string guid in guids)
                {
                    if (ProgramDataService.Configs.TryGetGameServer(guid, out IGameServer? gameServer) && !Instances.Select(x => x.Config.Guid).Contains(gameServer.Config.Guid))
                    {
                        AddInstance(gameServer, false);
                    }
                }
            }

            foreach (string path in ProgramDataService.Configs.GetFiles())
            {
                if (ProgramDataService.Configs.TryGetGameServer(Path.GetFileName(path), out IGameServer? gameServer) && !Instances.Select(x => x.Config.Guid).Contains(gameServer.Config.Guid))
                {
                    AddInstance(gameServer, false);
                }
            }

            ProgramDataService.Data.ServersOrder.Write(Instances.Select(x => x.Config.Guid.ToString()).ToList());
        }

        public static void AddInstance(IGameServer gameServer, bool updateOrder = true)
        {
            Instances.Add(gameServer);

            string path = ProgramDataService.Logs.GetPath(gameServer.Config.Guid);
            gameServer.Logger = new LoggerConfiguration().WriteTo.File(Path.Combine(path, "log.txt"), rollingInterval: RollingInterval.Day, rollOnFileSizeLimit: true).CreateLogger();
            gameServer.Status = string.IsNullOrEmpty(gameServer.Config.LocalVersion) ? Status.NotInstalled : Status.Stopped; 
            gameServer.Process.Exited += async (exitCode) => await gameServer.OnExited(gameServer);

            if (updateOrder)
            {
                ProgramDataService.Data.ServersOrder.Write(Instances.Select(x => x.Config.Guid.ToString()).ToList());
            }

            GameServersHasChanged?.Invoke();
        }

        public static void RemoveInstance(IGameServer gameServer)
        {
            Instances.Remove(gameServer);
            GameServersHasChanged?.Invoke();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _protocolTimer = new Timer(QueryGameServers, null, TimeSpan.Zero, TimeSpan.FromSeconds(10));

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _protocolTimer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        private async void QueryGameServers(object? state)
        {
            List<IGameServer> queryTasks = Instances.Where(x => x.Protocol != null && x.Status == Status.Started).ToList();

            _logger.LogInformation($"Query Servers: Start {queryTasks.Count}");

            await Parallel.ForEachAsync(queryTasks, async (gameServer, token) =>
            {
                try
                {
                    gameServer.Response = await gameServer.Protocol!.Query((IProtocolConfig)gameServer.Config);
                }
                catch
                {

                }
            });

            _logger.LogInformation("Query Servers: Done");
        }
        
        /// <inheritdoc/>
        public void Dispose()
        {
            _protocolTimer?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
