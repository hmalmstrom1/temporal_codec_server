using System.Threading.Tasks;

namespace TemporalCodecServer.KeyManagement
{
    public interface IKeyManagement
    {
        Task<byte[]> GetEncryptionKeyAsync(string keyId);
        Task<byte[]> GetDecryptionKeyAsync(string keyId);
    }
}
