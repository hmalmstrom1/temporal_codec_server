using Amazon.KeyManagementService;
using Amazon.KeyManagementService.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TemporalCodecServer.Encryption
{
    public class KmsEncryptionProvider : IEncryptionProvider
    {
        private readonly IAmazonKeyManagementService _kms;
        public KmsEncryptionProvider(IAmazonKeyManagementService kms)
        {
            _kms = kms;
        }

        public async Task<byte[]> EncryptAsync(byte[] plain, byte[] key, string keyId, IDictionary<string, string> metadata)
        {
            var request = new EncryptRequest
            {
                KeyId = keyId, // Should be the KMS KeyId or ARN
                Plaintext = new System.IO.MemoryStream(plain)
            };
            var response = await _kms.EncryptAsync(request);
            return response.CiphertextBlob.ToArray();
        }

        public async Task<byte[]> DecryptAsync(byte[] cipher, byte[] key, string keyId, IDictionary<string, string> metadata)
        {
            var request = new DecryptRequest
            {
                CiphertextBlob = new System.IO.MemoryStream(cipher)
            };
            var response = await _kms.DecryptAsync(request);
            return response.Plaintext.ToArray();
        }
    }
}
