using GameServerManager.GameServers.Components;
using GameServerManager.GameServers.Configs;
using GameServerManager.GameServers.Mods;
using System.Collections.Concurrent;
using System.Reflection;
using static GameServerManager.GameServers.Components.SteamCMD;

namespace GameServerManager.Services
{
    public class ContentVersionService : IHostedService, IDisposable
    {
        public interface IContentVersion
        {
            public List<string> Versions { get; set; }

            public DateTime DateTime { get; set; }

            public string LatestVersion => Versions.Count <= 0 ? string.Empty : Versions[0];
        }

        public class ContentVersion : IContentVersion
        {
            public List<string> Versions { get; set; } = new();

            public DateTime DateTime { get; set; } = DateTime.Now;
        }

        public static ConcurrentDictionary<Type, IContentVersion> Versions { get; private set; } = new();
        private readonly ILogger<ContentVersionService> _logger;
        private Timer? _versionsTimer;

        public ContentVersionService(ILogger<ContentVersionService> logger)
        {
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _versionsTimer = new Timer(FetchLatestVersions, null, TimeSpan.Zero, TimeSpan.FromSeconds(60 * 5));

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _versionsTimer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public static IContentVersion GetContentVersion(IGameServer gameServer)
        {
            if (typeof(ISteamCMDConfig).IsAssignableFrom(gameServer.Config.GetType()))
            {
                return new ContentVersion() { Versions = new() { GetRemoteBuildId(gameServer) } };
            }
            else
            {
                return Versions.GetValueOrDefault(gameServer.GetType()) ?? new ContentVersion();
            }
        }

        public static IContentVersion GetContentVersion(IMod mod)
        {
            return Versions.GetValueOrDefault(mod.GetType()) ?? new ContentVersion();
        }

        private async void FetchLatestVersions(object? state)
        {
            await FetchBranches();

            List<IVersionable> versionables = Assembly.GetExecutingAssembly().GetTypes()
                .Where(x => x.GetInterfaces().Contains(typeof(IVersionable)) && !x.IsAbstract)
                .Select(x => (Activator.CreateInstance(x) as IVersionable)!).ToList();

            await Parallel.ForEachAsync(versionables, async (versionable, token) =>
            {
                Type type = versionable.GetType();

                try
                {
                    ContentVersion version = new()
                    {
                        Versions = await versionable.GetVersions()
                    };

                    Versions[type] = version;

                    _logger.LogInformation("FetchLatestVersions: {version} ({type})", version.Versions[0], type.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogError("FetchLatestVersions: Fail to fetch ({type}) {ex}", type.Name, ex);
                }
            });
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _versionsTimer?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
