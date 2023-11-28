using System.Threading.Tasks;

namespace Function.Domain.Providers
{
    public interface IOlMessageProvider
    {
        bool IsEnabled { get; }
        Task SaveAsync(string content);
    }
}