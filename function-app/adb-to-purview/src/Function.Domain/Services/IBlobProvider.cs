using Azure.Storage.Blobs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Function.Domain.Services
{
    public interface IBlobProvider
    {
        Task<bool> BlobExistsAsync(string containerName, string blobName);

        Task UploadAsync(string containerName, string blobName, string jsonObject);

        Task UploadBinaryAsync(string containerName, string blobName, BinaryData binaryData, bool overwrite);

        Task<List<string>> GetBlobsByHierarchyAsync(string folderPrefix, string containerName);

        Task<string> DownloadBlobAsync(string containerName, string blobName);
    }
}