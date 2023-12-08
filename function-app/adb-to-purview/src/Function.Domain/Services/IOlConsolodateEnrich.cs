using System.Threading.Tasks;
using Function.Domain.Models.OL;

namespace Function.Domain.Services
{
    public interface IOlConsolodateEnrich<TEvent>where TEvent : class, IEnrichedEvent
    {
        public Task<TEvent?> ProcessOlMessage(string strEvent);
        public string GetJobNamespace();
    }
}