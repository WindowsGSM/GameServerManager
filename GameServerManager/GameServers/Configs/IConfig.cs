using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using GameServerManager.Services;
using GameServerManager.Utilities;

namespace GameServerManager.GameServers.Configs
{
    public interface IConfig
    {
        public string LocalVersion { get; set; }

        public string ClassName { get; }
        
        public Guid Guid { get; set; }

        public BasicConfig Basic { get; set; }

        public AdvancedConfig Advanced { get; set; }

        public BackupConfig Backup { get; set; }

        public bool Exists()
        {
            return File.Exists(ProgramDataService.Configs.GetPath(Guid));
        }

        public Task Update()
        {
            string contents = JsonSerializer.Serialize<dynamic>(this, new JsonSerializerOptions { WriteIndented = true });

            return File.WriteAllTextAsync(ProgramDataService.Configs.GetPath(Guid), contents);
        }

        public Task Delete()
        {
            return FileEx.DeleteAsync(ProgramDataService.Configs.GetPath(Guid));
        }

        public async Task<IConfig> Clone()
        {
            string json = await File.ReadAllTextAsync(ProgramDataService.Configs.GetPath(Guid));

            return (IConfig)JsonSerializer.Deserialize(json, GetType())!;
        }

        public bool TryGetPropertyInfo(string memberName, [NotNullWhen(true)] out PropertyInfo? tab)
        {
            tab = GetType().GetProperties().FirstOrDefault(x => x.Name.Equals(memberName));
            
            return tab != null;
        }
    }
}
