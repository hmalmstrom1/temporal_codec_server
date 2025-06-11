using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace TemporalCodecServer.Encryption
{
    public class AesEncryptionProvider : IEncryptionProvider
    {
        public Task<byte[]> EncryptAsync(byte[] plain, byte[] key, string keyId, IDictionary<string, string> metadata)
        {
            using var aes = Aes.Create();
            aes.Key = key;
            aes.GenerateIV();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream();
            ms.Write(aes.IV, 0, aes.IV.Length); // prepend IV
            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            {
                cs.Write(plain, 0, plain.Length);
                cs.FlushFinalBlock();
            }
            return Task.FromResult(ms.ToArray());
        }

        public Task<byte[]> DecryptAsync(byte[] cipher, byte[] key, string keyId, IDictionary<string, string> metadata)
        {
            using var aes = Aes.Create();
            aes.Key = key;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            var iv = new byte[16];
            Array.Copy(cipher, 0, iv, 0, 16);
            aes.IV = iv;
            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream(cipher, 16, cipher.Length - 16);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var plainMs = new MemoryStream();
            cs.CopyTo(plainMs);
            return Task.FromResult(plainMs.ToArray());
        }
    }
}
