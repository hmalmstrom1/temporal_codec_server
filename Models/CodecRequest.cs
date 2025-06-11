using System.Text.Json.Serialization;

namespace TemporalCodecServer.Models
{
    public class CodecRequest
    {
        [JsonPropertyName("payloads")]
        public List<Payload> Payloads { get; set; }

        [JsonPropertyName("metadata")]
        public Dictionary<string, string> Metadata { get; set; }
    }

    public class Payload
    {
        [JsonPropertyName("data")]
        public string Data { get; set; }

        [JsonPropertyName("metadata")]
        public Dictionary<string, string> Metadata { get; set; }
    }
}
