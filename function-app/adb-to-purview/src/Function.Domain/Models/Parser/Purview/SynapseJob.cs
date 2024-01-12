using Newtonsoft.Json;
namespace Function.Domain.Models.Purview
{
    public class SynapseJob
    {
        [JsonProperty("typeName")]
        public string TypeName = "synapse_job";
        [JsonProperty("attributes")]
        public SynapseJobAttributes Attributes = new SynapseJobAttributes();
        [JsonProperty("relationshipAttributes")]
        public SynapseJobRelationshipAttributes RelationshipAttributes = new SynapseJobRelationshipAttributes();
    }
}