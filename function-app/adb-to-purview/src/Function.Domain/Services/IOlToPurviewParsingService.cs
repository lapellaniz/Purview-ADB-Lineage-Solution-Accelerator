using System.Threading.Tasks;
using Function.Domain.Models.OL;

namespace Function.Domain.Services
{
    public interface IOlToPurviewParsingService
    {
        public string? GetPurviewFromOlEvent(EnrichedEvent eventData);

        public string? GetParentEntity(Event eventData);
        public string? GetChildEntity(Event eventData);
    }
}