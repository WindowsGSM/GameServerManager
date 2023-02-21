using GameServerManager.GameServers.Configs;

namespace GameServerManager.GameServers.Protocols
{
    public interface IQueryProtocol
    {
        public Task<IQueryResponse> Query(IProtocolConfig protocolConfig);
    }
}
