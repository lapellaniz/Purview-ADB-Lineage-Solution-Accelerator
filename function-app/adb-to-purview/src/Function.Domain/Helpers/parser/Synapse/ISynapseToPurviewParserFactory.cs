using Function.Domain.Models.OL;

namespace Function.Domain.Helpers.Parsers.Synapse
{
    public interface ISynapseToPurviewParserFactory
    {
        ISynapseToPurviewParser Create(EnrichedSynapseEvent synapseEvent);
    }
}