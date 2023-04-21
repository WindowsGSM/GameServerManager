using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using GameServerManager.Services;
using GameServerManager.Utilities;

namespace GameServerManager.GameServers.Configs
{
    public interface IModConfig
    {
        public string LocalVersion { get; set; }

        public bool TryGetPropertyInfo(string memberName, [NotNullWhen(true)] out PropertyInfo? tab)
        {
            tab = GetType().GetProperties().FirstOrDefault(x => x.Name.Equals(memberName));
            
            return tab != null;
        }
    }
}
