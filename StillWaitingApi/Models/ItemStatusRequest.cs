using Newtonsoft.Json;

namespace StillWaitingApi.Models
{
    public class ItemStatusRequest()
    {
        [JsonProperty("id")]
        public string? Id { get; set; }
    }
}