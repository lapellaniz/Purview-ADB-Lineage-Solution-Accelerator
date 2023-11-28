using Azure.Storage.Blobs;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using System;
using Microsoft.Extensions.Logging;

namespace Function.Domain.Services
{
    public class BlobClientFactory : IBlobClientFactory
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly BlobContainerClient _blobContainerClient;

        public BlobClientFactory(IConfiguration config, ILogger<BlobClientFactory> logger)
        {
            var tenantId = config["TenantId"];
            var msiClientId = config["UserMsiId"];
            var accountName = config["OlMessageStoreAccountName"];

            var credentialConfig = new DefaultAzureCredentialOptions()
            {
                TenantId = tenantId,
                ExcludeAzureCliCredential = false
            };
            if (Convert.ToBoolean(config["UseUserMsi"] ?? "false"))
            {
                credentialConfig.ManagedIdentityClientId = msiClientId;
            }
            _blobServiceClient = new(
                new Uri($"https://{accountName}.blob.core.windows.net"),
                new DefaultAzureCredential(credentialConfig));
            _blobContainerClient = _blobServiceClient.GetBlobContainerClient("ol-messages");
            _blobContainerClient.CreateIfNotExists(Azure.Storage.Blobs.Models.PublicAccessType.None);
            logger.LogInformation("BlobClientFactory created for {accountName}", accountName);
        }

        public BlobClient Create(string name)
        {
            return _blobContainerClient.GetBlobClient(name);
        }
    }
}