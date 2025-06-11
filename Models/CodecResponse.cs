using System.Text.Json.Serialization;

namespace TemporalCodecServer.Models
{
    public class CodecResponse
    {
        [JsonPropertyName("payloads")]
        public List<Payload> Payloads { get; set; }
    }
}
