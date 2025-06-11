using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Threading.Tasks;
using TemporalCodecServer.KeyManagement;
using TemporalCodecServer.Models;
using System.Text;

namespace TemporalCodecServer.Controllers
{
    [ApiController]
    [Route("/")]
    [Authorize]
    public class CodecController : ControllerBase
    {
        private readonly ILogger<CodecController> _logger;
        private readonly IKeyManagement _keyManagement;
        private readonly Encryption.IEncryptionProvider _encryptionProvider;

        public CodecController(ILogger<CodecController> logger, IKeyManagement keyManagement, Encryption.IEncryptionProvider encryptionProvider)
        {
            _logger = logger;
            _keyManagement = keyManagement;
            _encryptionProvider = encryptionProvider;
        }

        [HttpPost("encode")]
        public async Task<IActionResult> Encode([FromBody] CodecRequest request)
        {
            var key = await _keyManagement.GetEncryptionKeyAsync("default");
            var encodedPayloads = new List<Payload>();
            foreach (var payload in request.Payloads)
            {
                var plainBytes = Convert.FromBase64String(payload.Data);
                var encryptedBytes = await _encryptionProvider.EncryptAsync(plainBytes, key, "default", payload.Metadata);
                var encodedData = Convert.ToBase64String(encryptedBytes);
                encodedPayloads.Add(new Payload
                {
                    Data = encodedData,
                    Metadata = payload.Metadata
                });
            }
            var response = new CodecResponse { Payloads = encodedPayloads };
            return Ok(response);
        }

        [HttpPost("decode")]
        public async Task<IActionResult> Decode([FromBody] CodecRequest request)
        {
            var key = await _keyManagement.GetDecryptionKeyAsync("default");
            var decodedPayloads = new List<Payload>();
            foreach (var payload in request.Payloads)
            {
                var encryptedBytes = Convert.FromBase64String(payload.Data);
                var decryptedBytes = await _encryptionProvider.DecryptAsync(encryptedBytes, key, "default", payload.Metadata);
                var decodedData = Convert.ToBase64String(decryptedBytes);
                decodedPayloads.Add(new Payload
                {
                    Data = decodedData,
                    Metadata = payload.Metadata
                });
            }
            var response = new CodecResponse { Payloads = decodedPayloads };
            return Ok(response);
        }

        [AllowAnonymous]
        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new { status = "ok" });
        }
    }
}
