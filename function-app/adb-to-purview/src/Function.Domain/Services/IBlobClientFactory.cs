using Azure.Storage.Blobs;

namespace Function.Domain.Services
{
    public interface IBlobClientFactory
    {
        BlobClient Create(string name);
    }
}