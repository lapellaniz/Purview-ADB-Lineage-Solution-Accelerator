using System;
using System.Threading.Tasks;
using Function.Domain.Services;

namespace Function.Domain.Providers
{
    public class OlMessageProvider : IOlMessageProvider
    {
        private readonly IBlobClientFactory _blobClientFactory;
        public OlMessageProvider(IBlobClientFactory blobClientFactory)
        {
            _blobClientFactory = blobClientFactory;
        }

        public bool IsEnabled => true;

        public async Task SaveAsync(string content)
        {
            var blobClient = _blobClientFactory.Create("ol-messages", $"{DateTime.UtcNow:s}_{Guid.NewGuid()}.json");
            await blobClient.UploadAsync(BinaryData.FromString(content), overwrite: true);
        }
    }
}