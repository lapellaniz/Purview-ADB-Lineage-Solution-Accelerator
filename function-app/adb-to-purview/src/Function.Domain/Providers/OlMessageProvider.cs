using System;
using System.Threading.Tasks;
using Function.Domain.Services;

namespace Function.Domain.Providers
{
    public class OlMessageProvider : IOlMessageProvider
    {
        private readonly IBlobProvider _blobProvider;
        public OlMessageProvider(IBlobProvider blobProvider)
        {
            _blobProvider = blobProvider;
        }

        public bool IsEnabled => true;

        public async Task SaveAsync(string content)
        {
            await this._blobProvider.UploadBinaryAsync("ol-messages", $"{DateTime.UtcNow:s}_{Guid.NewGuid()}.json", BinaryData.FromString(content), overwrite: true);
        }
    }
}