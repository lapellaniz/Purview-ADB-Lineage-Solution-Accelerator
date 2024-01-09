using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using System.IO;
using Azure.Storage.Blobs.Models;
using System;

namespace Function.Domain.Services
{
    public class BlobProvider : IBlobProvider
    {
        private IBlobClientFactory blobClientFactory;

        public BlobProvider(IBlobClientFactory blobClientFactory)
        {
            this.blobClientFactory = blobClientFactory;
        }

        public async Task<bool> BlobExistsAsync(string containerName, string blobName)
        {
            var blobClient = await this.blobClientFactory.GetBlobClientAsync(containerName, blobName);
            return await blobClient.ExistsAsync();
        }

        public async Task UploadAsync(string containerName, string blobName, string jsonObject)
        {
            var blobClient = await this.blobClientFactory.GetBlobClientAsync(containerName, blobName);
            byte[] data = Encoding.UTF8.GetBytes(jsonObject);
            using (var stream = new MemoryStream(data))
            {
                await blobClient.UploadAsync(stream);
            }
        }

        public async Task UploadBinaryAsync(string containerName, string blobName, BinaryData binaryData, bool overwrite = false)
        {
            var blobClient = await this.blobClientFactory.GetBlobClientAsync(containerName, blobName);
            await blobClient.UploadAsync(binaryData, overwrite: overwrite);
        }

        public async Task<List<string>> GetBlobsByHierarchyAsync(string folderPrefix, string containerName)
        {
            List<string> blobNames = new List<string>();
            var containerClient = await this.blobClientFactory.GetBlobContainerClientAsync(containerName);
            await foreach (var blobItem in containerClient.GetBlobsByHierarchyAsync(prefix: folderPrefix))
            {
                blobNames.Add(blobItem.Blob.Name);
            }

            return blobNames;
        }

        public async Task<string> DownloadBlobAsync(string containerName, string blobName)
        {
            var blobClient = await this.blobClientFactory.GetBlobClientAsync(containerName, blobName);
            using (MemoryStream stream = new MemoryStream())
            {
                await blobClient.DownloadToAsync(stream);
                stream.Position = 0;
                byte[] blobContent = stream.ToArray();
                return System.Text.Encoding.UTF8.GetString(blobContent);
            }
        }
    }
}