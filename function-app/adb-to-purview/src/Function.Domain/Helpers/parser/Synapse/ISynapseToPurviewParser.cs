using System.Threading.Tasks;
using Function.Domain.Models.Purview;

namespace Function.Domain.Helpers.Parsers.Synapse
{
    public interface ISynapseToPurviewParser
    {
        public SynapseWorkspace GetSynapseWorkspace();

        public SynapseNotebook GetSynapseNotebook(string workspaceQn);

        public Task<SynapseProcess> GetSynapseProcessAsync(string sparkNotebookQn, SynapseNotebook synapseNotebook);
    }
}