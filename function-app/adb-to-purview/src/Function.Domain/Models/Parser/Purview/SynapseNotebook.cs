using Newtonsoft.Json;
namespace Function.Domain.Models.Purview
{
    public class SynapseNotebook
    {
        [JsonProperty("typeName")]
        public string TypeName = "azure_synapse_notebook";
        [JsonProperty("attributes")]
        public SynapseNotebookAttributes Attributes = new SynapseNotebookAttributes();
        [JsonProperty("relationshipAttributes")]
        public SynapseNotebookRelationshipAttributes RelationshipAttributes = new SynapseNotebookRelationshipAttributes();
    }
}