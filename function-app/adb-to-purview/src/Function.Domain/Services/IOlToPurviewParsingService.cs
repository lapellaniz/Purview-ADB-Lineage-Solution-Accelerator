using System.Threading.Tasks;
using Function.Domain.Models.OL;

namespace Function.Domain.Services
{
    public interface IOlToPurviewParsingService
    {
        public string? GetPurviewFromOlEvent(EnrichedEvent eventData);

        public string? GetParentEntity(EnrichedSynapseEvent eventData);
        public string? GetChildEntity(EnrichedSynapseEvent eventData);
    }
}