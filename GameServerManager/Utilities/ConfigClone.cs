using GameServerManager.GameServers.Configs;

namespace GameServerManager.Utilities
{
    public class ConfigClone : ICloneable<IConfig>
    {
        public IConfig Config { get; init; }

        public ConfigClone(IConfig config)
        {
            Config = config;
        }

        public IConfig Clone()
        {
            return ((ConfigClone)MemberwiseClone()).Config;
        }
    }

    public interface ICloneable<T>
    {
        public T Clone();
    }
}
