﻿using GameServerManager.GameServers.Configs;
using GameServerManager.GameServers.Engines;

namespace GameServerManager.GameServers
{
    /// <summary>
    /// Age of Chivalry Dedicated Server
    /// </summary>
    public class AOC : SourceEngine
    {
        public override string Name => "Age of Chivalry Dedicated Server";

        public override string ImageSource => $"/images/games/{nameof(AOC)}.jpg";

        public override IConfig Config { get; set; } = new Configuration()
        {
            ClassName = nameof(AOC),
            Start =
            {
                StartParameter = "-console -game ageofchivalry +ip 0.0.0.0 -port 27015 +maxplayers 32 +map aoc_siege"
            },
            Backup =
            {
                Entries =
                {
                    "ageofchivalry\\addons",
                    "ageofchivalry\\cfg",
                    "ageofchivalry\\maps"
                },
            },
            SteamCMD =
            {
                Game = "ageofchivalry",
                AppId = "17515",
                Username = "anonymous"
            }
        };
    }
}
