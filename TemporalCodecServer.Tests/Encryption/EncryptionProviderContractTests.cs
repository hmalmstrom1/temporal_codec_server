using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Moq;
using TemporalCodecServer.Encryption;
using TemporalCodecServer.KeyManagement;
using TemporalCodecServer.Tests.TestHelpers;
using Xunit;

namespace TemporalCodecServer.Tests.Encryption
{
    public abstract class EncryptionProviderContractTests
    {
        protected abstract IEncryptionProvider CreateProvider();
        protected abstract byte[] CreateTestKey();

        [Fact]
        public async Task EncryptThenDecrypt_ReturnsOriginalData()
        {
            // Arrange
            var provider = CreateProvider();
            var key = CreateTestKey();
            var original = Encoding.UTF8.GetBytes("Test message");
            var metadata = new Dictionary<string, string> { { "key1", "value1" } };

            // Act
            var encrypted = await provider.EncryptAsync(original, key, "test-key", metadata);
            var decrypted = await provider.DecryptAsync(encrypted, key, "test-key", metadata);

            // Assert
            Assert.Equal(original, decrypted);
        }

        [Fact]
        public async Task EncryptThenDecrypt_WithEmptyData_WorksCorrectly()
        {
            // Arrange
            var provider = CreateProvider();
            var key = CreateTestKey();
            var original = Array.Empty<byte>();
            var metadata = new Dictionary<string, string>();

            // Act
            var encrypted = await provider.EncryptAsync(original, key, "test-key", metadata);
            var decrypted = await provider.DecryptAsync(encrypted, key, "test-key", metadata);

            // Assert
            Assert.Empty(decrypted);
        }

        [Fact]
        public async Task EncryptThenDecrypt_WithSameKey_ReturnsOriginalData()
        {
            // Arrange
            var provider = CreateProvider();
            var key = CreateTestKey();
            var original = Encoding.UTF8.GetBytes("Test message");
            var metadata = new Dictionary<string, string> { { "test", "value" } };

            // Act
            var encrypted = await provider.EncryptAsync(original, key, "test-key", metadata);
            var decrypted = await provider.DecryptAsync(encrypted, key, "test-key", metadata);

            // Assert
            Assert.Equal(original, decrypted);
        }
    }

    public class Base64EncryptionProviderContractTests : EncryptionProviderContractTests
    {
        protected override IEncryptionProvider CreateProvider() => new Base64EncryptionProvider();
        protected override byte[] CreateTestKey() => Encoding.UTF8.GetBytes("test-key-12345678");
    }

    public class AesEncryptionProviderContractTests : EncryptionProviderContractTests, IDisposable
    {
        private readonly System.Security.Cryptography.Aes _aes;

        public AesEncryptionProviderContractTests()
        {
            _aes = System.Security.Cryptography.Aes.Create();
            _aes.GenerateKey();
        }

        protected override IEncryptionProvider CreateProvider() => new AesEncryptionProvider();
        protected override byte[] CreateTestKey() => _aes.Key;

        public void Dispose()
        {
            _aes?.Dispose();
        }
    }
}
