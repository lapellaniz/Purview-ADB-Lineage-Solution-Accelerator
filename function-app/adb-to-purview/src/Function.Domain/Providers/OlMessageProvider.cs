using System;
using System.Threading.Tasks;
using Function.Domain.Services;
using Microsoft.FeatureManagement;

namespace Function.Domain.Providers
{
    public class OlMessageProvider : IOlMessageProvider
    {
        private readonly IBlobProvider _blobProvider;
        private readonly IFeatureManager _featureManager;
        public OlMessageProvider(IBlobProvider blobProvider, IFeatureManager featureManager)
        {
            _blobProvider = blobProvider;
            _featureManager = featureManager;
        }

        public async Task<bool> IsEnabledAsync()
        {
            return await _featureManager.IsEnabledAsync(Constants.FeatureFlags.Logging.LogOlMessageToExternalStore);
        }

        public async Task SaveAsync(string content)
        {
            await this._blobProvider.UploadBinaryAsync("ol-messages", $"{DateTime.UtcNow:s}_{Guid.NewGuid()}.json", BinaryData.FromString(content), overwrite: true);
        }
    }
}