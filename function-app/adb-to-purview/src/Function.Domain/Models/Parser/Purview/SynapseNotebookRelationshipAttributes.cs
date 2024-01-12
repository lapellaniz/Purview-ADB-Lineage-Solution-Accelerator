using Newtonsoft.Json;
namespace Function.Domain.Models.Purview
{
    public class SynapseNotebookRelationshipAttributes
    {
        [JsonProperty("workspace")]
        public RelationshipAttribute Workspace = new RelationshipAttribute();
    }
}