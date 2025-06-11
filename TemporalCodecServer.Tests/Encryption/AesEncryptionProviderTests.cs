using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Moq;
using TemporalCodecServer.Encryption;
using TemporalCodecServer.KeyManagement;
using TemporalCodecServer.Tests.TestHelpers;
using Xunit;

namespace TemporalCodecServer.Tests.Encryption
{
    public class AesEncryptionProviderTests : IDisposable
    {
        private readonly AesEncryptionProvider _provider;
        private readonly byte[] _testKey;
        private readonly Aes _aes;

        public AesEncryptionProviderTests()
        {
            _aes = Aes.Create();
            _aes.GenerateKey();
            _testKey = _aes.Key;
            _provider = new AesEncryptionProvider();
        }

        public void Dispose()
        {
            _aes?.Dispose();
            GC.SuppressFinalize(this);
        }

        [Fact]
        public async Task EncryptAsync_WithValidInput_ReturnsEncryptedData()
        {
            // Arrange
            var input = Encoding.UTF8.GetBytes("Test message");
            var metadata = new Dictionary<string, string> { { "key1", "value1" } };

            // Act
            var result = await _provider.EncryptAsync(input, _testKey, "test-key", metadata);

            // Assert - Should be different from input and have IV prepended
            Assert.NotEqual(input, result);
            Assert.True(result.Length > input.Length);
        }

        [Fact]
        public async Task DecryptAsync_WithValidInput_ReturnsOriginalData()
        {
            // Arrange
            var original = Encoding.UTF8.GetBytes("Test message");
            var metadata = new Dictionary<string, string> { { "key1", "value1" } };
            var encrypted = await _provider.EncryptAsync(original, _testKey, "test-key", metadata);

            // Act
            var decrypted = await _provider.DecryptAsync(encrypted, _testKey, "test-key", metadata);

            // Assert
            Assert.Equal(original, decrypted);
        }

        [Fact]
        public async Task EncryptThenDecrypt_WithEmptyData_WorksCorrectly()
        {
            // Arrange
            var original = Array.Empty<byte>();
            var metadata = new Dictionary<string, string>();

            // Act
            var encrypted = await _provider.EncryptAsync(original, _testKey, "test-key", metadata);
            var decrypted = await _provider.DecryptAsync(encrypted, _testKey, "test-key", metadata);

            // Assert
            Assert.Empty(decrypted);
        }

        [Fact]
        public async Task EncryptThenDecrypt_WithLargeData_WorksCorrectly()
        {
            // Arrange
            var rnd = new Random();
            var original = new byte[1024 * 1024]; // 1MB
            rnd.NextBytes(original);
            var metadata = new Dictionary<string, string> { { "size", "large" } };

            // Act
            var encrypted = await _provider.EncryptAsync(original, _testKey, "test-key", metadata);
            var decrypted = await _provider.DecryptAsync(encrypted, _testKey, "test-key", metadata);

            // Assert
            Assert.Equal(original, decrypted);
        }

        [Fact]
        public async Task Encrypt_WithSameInput_DifferentIv_ProducesDifferentOutputs()
        {
            // Arrange
            var input = Encoding.UTF8.GetBytes("Test message");
            var metadata = new Dictionary<string, string>();

            // Act
            var encrypted1 = await _provider.EncryptAsync(input, _testKey, "test-key", metadata);
            var encrypted2 = await _provider.EncryptAsync(input, _testKey, "test-key", metadata);

            // Assert - Different IVs should produce different ciphertexts
            Assert.NotEqual(encrypted1, encrypted2);
        }

        [Fact]
        public async Task Decrypt_WithTamperedCiphertext_ThrowsCryptographicException()
        {
            // Arrange
            var original = Encoding.UTF8.GetBytes("Test message");
            var metadata = new Dictionary<string, string>();
            var encrypted = await _provider.EncryptAsync(original, _testKey, "test-key", metadata);

            // Tamper with the ciphertext
            encrypted[encrypted.Length / 2] ^= 0xFF;

            // Act & Assert
            await Assert.ThrowsAsync<CryptographicException>(
                () => _provider.DecryptAsync(encrypted, _testKey, "test-key", metadata));
        }
    }
}
