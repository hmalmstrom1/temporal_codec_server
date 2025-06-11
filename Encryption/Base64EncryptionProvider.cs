using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TemporalCodecServer.Encryption
{
    // This is a dummy encryption provider that just base64 "encrypts" and "decrypts"
    public class Base64EncryptionProvider : IEncryptionProvider
    {
        public Task<byte[]> EncryptAsync(byte[] plain, byte[] key, string keyId, IDictionary<string, string> metadata)
        {
            // Just return the input for demo
            return Task.FromResult(plain);
        }
        public Task<byte[]> DecryptAsync(byte[] cipher, byte[] key, string keyId, IDictionary<string, string> metadata)
        {
            // Just return the input for demo
            return Task.FromResult(cipher);
        }
    }
}
