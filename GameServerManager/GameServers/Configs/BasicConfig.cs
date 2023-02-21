using GameServerManager.Attributes;
using GameServerManager.Services;

namespace GameServerManager.GameServers.Configs
{
    public class BasicConfig
    {
        [TextField(Label = "Name", HelperText = "Server Display Name", Required = true)]
        public string Name { get; set; } = string.Empty;

        [TextField(Label = "Directory", HelperText = "Server Files Directory", Required = true, FolderBrowser = true)]
        public string Directory { get; set; } = string.Empty;

        /// <summary>
        /// Create a new BasicConfig with name and directory initalized
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        public static BasicConfig CreateNew(Guid guid)
        {
            string[] names = GameServerService.Instances.Select(x => x.Config.Basic.Name).ToArray();
            int number = GameServerService.Instances.Count;

            while (names.Contains($"WindowsGSM - Server #{++number}")) ;

            return new()
            {
                Name = $"WindowsGSM - Server #{number}",
                Directory = Path.Combine(ProgramDataService.Servers.ServersPath, guid.ToString()),
            };
        }
    }
}
