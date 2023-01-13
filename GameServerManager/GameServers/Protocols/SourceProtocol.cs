using OpenGSQ.Protocols;
using GameServerManager.GameServers.Configs;
using GameServerManager.Utilities;

namespace GameServerManager.GameServers.Protocols
{
    public class SourceProtocol : IProtocol
    {
        public async Task<IResponse> Query(IProtocolConfig protocolConfig)
        {
            Source source = new(protocolConfig.Protocol.IPAddress, protocolConfig.Protocol.QueryPort);
            Source.IResponse response = await Task.Run(() => source.GetInfo());

            ProtocolResponse protocolResponse = new()
            {
                Name = response.Name,
                MapName = response.Map,
                Player = response.Players,
                MaxPlayer = response.MaxPlayers,
                Bot = response.Bots,
            };

            return protocolResponse;
        }
    }
}
