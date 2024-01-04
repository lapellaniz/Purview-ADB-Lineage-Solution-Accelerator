using Azure.Storage.Blobs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Function.Domain.Services
{
    public interface IBlobClientFactory
    {
        Task<BlobClient> GetBlobClientAsync(string containerName, string name);

        Task<BlobContainerClient> GetBlobContainerClientAsync(string containerName);
    }
}