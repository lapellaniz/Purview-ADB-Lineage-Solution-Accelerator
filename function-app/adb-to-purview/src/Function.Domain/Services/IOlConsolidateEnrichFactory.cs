using System.Threading.Tasks;
using Function.Domain.Models.OL;

namespace Function.Domain.Services
{
    public interface IOlConsolidateEnrichFactory
    {
        IOlConsolodateEnrich<TEvent> CreateEnrichment<TEvent>(OlEnrichmentType type) where TEvent : class, IEnrichedEvent;
    }
}