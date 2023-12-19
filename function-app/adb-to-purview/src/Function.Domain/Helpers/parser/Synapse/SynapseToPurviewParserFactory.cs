using Function.Domain.Helpers.Hash;
using Function.Domain.Models.OL;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Function.Domain.Helpers.Parsers.Synapse
{
    public class SynapseToPurviewParserFactory : ISynapseToPurviewParserFactory
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly IConfiguration _configuration;
        private readonly IPurviewAssetNameHashBroker _purviewAssetNameHashBroker;
        public SynapseToPurviewParserFactory(ILoggerFactory loggerFactory, IConfiguration configuration, IPurviewAssetNameHashBroker purviewAssetNameHashBroker)
        {
            _loggerFactory = loggerFactory ?? throw new System.ArgumentNullException(nameof(loggerFactory));
            _configuration = configuration ?? throw new System.ArgumentNullException(nameof(configuration));
            _purviewAssetNameHashBroker = purviewAssetNameHashBroker ?? throw new System.ArgumentNullException(nameof(purviewAssetNameHashBroker));
        }

        public ISynapseToPurviewParser Create(EnrichedSynapseEvent synapseEvent)
        {
            return new SynapseToPurviewParser(_loggerFactory, _configuration, synapseEvent, _purviewAssetNameHashBroker);
        }
    }
}