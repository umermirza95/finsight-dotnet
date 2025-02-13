using Finsight.Interface;
using Google.Cloud.SecretManager.V1;
using Microsoft.Extensions.Configuration;

namespace Finsight.Service
{
    public class GCPSecretsProvider : FSISecretsProvider
    {
        Dictionary<string, string> secretsCache;
        private readonly string gcpProjectId;
        private readonly SecretManagerServiceClient client;

        public GCPSecretsProvider(IConfiguration configuration)
        {
            secretsCache = [];
            client = SecretManagerServiceClient.Create();
            gcpProjectId = configuration["GCP_PROJECT_ID"] ?? throw new Exception("GCP Project Id is missing");
        }
        public async Task<string> GetSecretAsync(string secretName)
        {
            if (secretsCache.ContainsKey(secretName))
            {
                return secretsCache[secretName];
            }
            var secretVersion = new SecretVersionName(gcpProjectId, secretName, "latest");
            var result = await client.AccessSecretVersionAsync(secretVersion);
            return result.Payload.Data.ToStringUtf8();
        }
    }
}