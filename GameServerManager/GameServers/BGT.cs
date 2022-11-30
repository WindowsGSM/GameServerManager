using GameServerManager.GameServers.Configs;
using GameServerManager.GameServers.Engines;

namespace GameServerManager.GameServers
{
    public class BGT : SourceEngine
    {
        public override string Name => "Bloody Good Time Dedicated Server";

        public override string ImageSource => $"/images/games/{nameof(BGT)}.jpg";

        public override IConfig Config { get; set; } = new Configuration()
        {
            ClassName = nameof(BGT),
            Start =
            {
                StartParameter = ""
            },
            Backup =
            {
                Entries =
                {

                },
            },
            SteamCMD =
            {
                Game = "",
                AppId = "2460",
                Username = "anonymous",
            },
        };
    }
}
