using System.Threading.Tasks;

namespace Function.Domain.Providers
{
    public interface IOlMessageProvider
    {
        Task<bool> IsEnabledAsync();
        Task SaveAsync(string content);
    }
}