using Newtonsoft.Json;
namespace Function.Domain.Models.Purview
{
    public class SynapseJobRelationshipAttributes
    {
        [JsonProperty("workspace")]
        public RelationshipAttribute Workspace = new RelationshipAttribute();
    }
}