using System.Text.Json.Serialization;

namespace GameServerManager.Utilities.Steamworks
{
    public class ResponseWrapper<T>
    {
        [JsonPropertyName("response")]
        public T? Response { get; set; }
    }
}
