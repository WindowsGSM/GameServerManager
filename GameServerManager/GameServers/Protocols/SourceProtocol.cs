using OpenGSQ.Protocols;
using GameServerManager.GameServers.Configs;
using GameServerManager.Utilities;

namespace GameServerManager.GameServers.Protocols
{
    public class SourceProtocol : IQueryProtocol
    {
        public async Task<IQueryResponse> Query(IProtocolConfig protocolConfig)
        {
            Source source = new(protocolConfig.Protocol.IPAddress, protocolConfig.Protocol.QueryPort);
            Source.IResponse response = await Task.Run(() => source.GetInfo());

            QueryResponse protocolResponse = new()
            {
                Name = response.Name,
                Map = response.Map,
                Player = response.Players,
                MaxPlayer = response.MaxPlayers,
                Bot = response.Bots,
            };

            return protocolResponse;
        }
    }
}
