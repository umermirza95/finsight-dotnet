
namespace Finsight.Interface
{
    public interface FSISecretsProvider
    {
        Task<string> GetSecretAsync(string secretName);
    }
}