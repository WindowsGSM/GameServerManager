namespace GameServerManager.GameServers.Protocols
{
    public class QueryResponse : IQueryResponse
    {
        public string Name { get; set; } = string.Empty;

        public string Map { get; set; } = string.Empty;

        public int Player { get; set; }

        public int MaxPlayer { get; set; }

        public int Bot { get; set; }

        public DateTime DateTime { get; set; } = DateTime.Now;
    }
}
