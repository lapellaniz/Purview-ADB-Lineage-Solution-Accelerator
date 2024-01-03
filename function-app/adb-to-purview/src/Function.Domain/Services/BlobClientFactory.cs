using Azure.Storage.Blobs;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using System;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using System.IO;
using Azure.Storage.Blobs.Models;

namespace Function.Domain.Services
{
    public class BlobClientFactory : IBlobClientFactory
    {
        private readonly BlobServiceClient _blobServiceClient;
        private BlobContainerClient _blobContainerClient;

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
            //_blobContainerClient = _blobServiceClient.GetBlobContainerClient("ol-messages");
            //_blobContainerClient.CreateIfNotExists(Azure.Storage.Blobs.Models.PublicAccessType.None);
            logger.LogInformation("BlobClientFactory created for {accountName}", accountName);
        }

        public BlobClient Create(string containerName, string name)
        {
            GetBlobContainer(containerName);
            return _blobContainerClient.GetBlobClient(name);
        }

        public async Task UploadAsync(string containerName, string blobName, string jsonObject)
        {
            var blobClient = Create(containerName, blobName);
            byte[] data = Encoding.UTF8.GetBytes(jsonObject);
            using (var stream = new MemoryStream(data))
            {
                await blobClient.UploadAsync(stream, new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders { ContentType = "application/json" },
                });
            }
        }

        public async Task<List<string>> GetBlobsByHierarchyAsync(string folderPrefix)
        {
            List<string> blobNames = new List<string>();
            await foreach (var blobItem in _blobContainerClient.GetBlobsByHierarchyAsync(prefix: folderPrefix))
            {
                blobNames.Add(blobItem.Blob.Name);
            }

            return blobNames;
        }

        public async Task<string> DownloadBlobAsync(string containerName, string blobName)
        {
            var blobClient = Create(containerName, blobName);
            using (MemoryStream stream = new MemoryStream())
            {
                await blobClient.DownloadToAsync(stream);
                stream.Position = 0;
                byte[] blobContent = stream.ToArray();
                return System.Text.Encoding.UTF8.GetString(blobContent);
            }
        }

        private void GetBlobContainer(string containerName)
        {
            _blobContainerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            _blobContainerClient.CreateIfNotExists(PublicAccessType.None);
        }
    }
}