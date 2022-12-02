using Newtonsoft.Json;
namespace Function.Domain.Models.Purview
{
    public class DatabricksNotebook
    {
        [JsonProperty("typeName")]
        public string TypeName = "databricks_notebook";
        [JsonProperty("attributes")]
        public DatabricksNotebookAttributes Attributes = new DatabricksNotebookAttributes();
        [JsonProperty("relationshipAttributes")]
        public DatabricksNotebookRelationshipAttributes RelationshipAttributes = new DatabricksNotebookRelationshipAttributes();
    }


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