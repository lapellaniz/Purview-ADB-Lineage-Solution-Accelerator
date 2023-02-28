using System.Threading.Tasks;
using Function.Domain.Models.SynapseSpark;

namespace Function.Domain.Providers
{
    public interface ISynapseClientProvider
    {
        public Task<SynapseRoot?> GetSynapseJobAsync(long runId, string synapseWorkspaceName);
        public Task<SynapseSparkPool?> GetSynapseSparkPoolsAsync(string synapseWorkspaceName, string synapseSparkPoolName);

        public Task<string> GetSparkNotebookSource(string synapseWorkspaceName, string sparkNoteBookName);
    }

}