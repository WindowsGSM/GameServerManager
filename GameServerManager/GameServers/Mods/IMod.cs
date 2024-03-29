﻿using GameServerManager.GameServers.Components;
using GameServerManager.GameServers.Configs;

namespace GameServerManager.GameServers.Mods
{
    public interface IMod : IVersionable
    {
        public string Name { get; }

        public string Description { get; }

        public Type ConfigType { get; }

        public IModConfig GetModConfig(IGameServer gameServer);

        public Task Install(IGameServer gameServer, string version);

        public Task Update(IGameServer gameServer, string version);

        public Task Delete(IGameServer gameServer);
    }
}
