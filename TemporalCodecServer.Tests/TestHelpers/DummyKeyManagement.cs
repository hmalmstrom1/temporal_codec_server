using System;
using System.Text;
using System.Threading.Tasks;
using TemporalCodecServer.KeyManagement;

namespace TemporalCodecServer.Tests.TestHelpers
{
    public class DummyKeyManagement : IKeyManagement
    {
        private readonly byte[] _key;

        public DummyKeyManagement() : this(Encoding.UTF8.GetBytes("default-test-key-12345678"))
        {
        }

        public DummyKeyManagement(byte[] key)
        {
            _key = key ?? throw new ArgumentNullException(nameof(key));
        }

        public Task<byte[]> GetEncryptionKeyAsync(string keyId)
        {
            return Task.FromResult(_key);
        }

        public Task<byte[]> GetDecryptionKeyAsync(string keyId)
        {
            return Task.FromResult(_key);
        }
    }
}
