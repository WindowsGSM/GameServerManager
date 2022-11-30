using GameServerManager.GameServers.Configs;

namespace GameServerManager.GameServers.Protocols
{
    public interface IProtocol
    {
        public Task<IResponse> Query(IProtocolConfig protocolConfig);
    }
}
