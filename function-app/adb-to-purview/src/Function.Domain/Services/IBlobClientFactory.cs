using Azure.Storage.Blobs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Function.Domain.Services
{
    public interface IBlobClientFactory
    {
        BlobClient Create(string containerName, string name);

        Task UploadAsync(string containerName, string blobName, string jsonObject);

        Task<List<string>> GetBlobsByHierarchyAsync(string folderPrefix);

        Task<string> DownloadBlobAsync(string containerName, string blobName);
    }
}