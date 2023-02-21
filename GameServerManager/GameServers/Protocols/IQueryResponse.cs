namespace GameServerManager.GameServers.Protocols
{
    public interface IQueryResponse
    {
        public string Name { get; set; }

        public string Map { get; set; }

        public int Player { get; set; }

        public int MaxPlayer { get; set; }

        public int Bot { get; set; }

        public DateTime DateTime { get; set; }
    }
}
