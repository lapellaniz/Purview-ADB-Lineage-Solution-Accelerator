using Newtonsoft.Json;
namespace Function.Domain.Models.Purview
{
    public class DatabricksNotebookRelationshipAttributes
    {
        [JsonProperty("workspace")]
        public RelationshipAttribute Workspace = new RelationshipAttribute();
    }

    public class SynapseNotebookRelationshipAttributes
    {
        [JsonProperty("workspace")]
        public RelationshipAttribute Workspace = new RelationshipAttribute();
    }
}