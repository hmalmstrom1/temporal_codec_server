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
    public class Base64EncryptionProviderTests : IDisposable
    {
        private readonly Base64EncryptionProvider _provider;
        private readonly IKeyManagement _keyManagement;
        private readonly byte[] _testKey;

        public Base64EncryptionProviderTests()
        {
            _keyManagement = new DummyKeyManagement();
            _testKey = Encoding.UTF8.GetBytes("test-key-12345678");
            _provider = new Base64EncryptionProvider();
        }

        public void Dispose()
        {
            // Clean up if needed
        }

        [Fact]
        public async Task EncryptAsync_WithValidInput_ReturnsSameBytes()
        {
            // Arrange
            var input = Encoding.UTF8.GetBytes("Test message");
            var metadata = new Dictionary<string, string> { { "key1", "value1" } };

            // Act
            var result = await _provider.EncryptAsync(input, _testKey, "test-key", metadata);

            // Assert
            Assert.Equal(input, result);
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
    }
}
