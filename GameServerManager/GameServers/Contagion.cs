﻿using GameServerManager.GameServers.Configs;
using GameServerManager.GameServers.Engines;

namespace GameServerManager.GameServers
{
    public class Contagion : SourceEngine
    {
        public override string Name => "Contagion Dedicated Server";

        public override string ImageSource => $"/images/games/{nameof(Contagion)}.jpg";

        public override IConfig Config { get; set; } = new Configuration()
        {
            ClassName = nameof(Contagion),
            Start =
            {
                StartParameter = "-console -game contagion -ip 0.0.0.0 -port 27015 -maxplayers 8 +map ch_cypruspark",
            },
            SteamCMD =
            {
                Game = "contagion",
                AppId = "293030",
                Username = "anonymous"
            }
        };
    }
}
