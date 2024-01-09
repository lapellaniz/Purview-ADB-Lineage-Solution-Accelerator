using System.Threading.Tasks;
using Function.Domain.Models.OL;

namespace Function.Domain.Helpers
{
    public interface IOlMessageEnrichment
    {
        Task<Event?> EnrichmentEventAsync(Event olEvent, string workspaceName);
    }
}