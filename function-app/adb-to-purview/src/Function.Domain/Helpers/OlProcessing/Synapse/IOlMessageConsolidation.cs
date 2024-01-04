using System.Threading.Tasks;
using Function.Domain.Models.OL;

namespace Function.Domain.Helpers
{
    public interface IOlMessageConsolidation
    {
        Task<Event?> ConsolidateEventAsync(Event olEvent, string runId);
    }
}