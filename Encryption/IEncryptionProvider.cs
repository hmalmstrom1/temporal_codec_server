using System.Threading.Tasks;

namespace TemporalCodecServer.Encryption
{
    public interface IEncryptionProvider
    {
        Task<byte[]> EncryptAsync(byte[] plain, byte[] key, string keyId, IDictionary<string, string> metadata);
        Task<byte[]> DecryptAsync(byte[] cipher, byte[] key, string keyId, IDictionary<string, string> metadata);
    }
}
