using Newtonsoft.Json;
namespace Function.Domain.Models.Purview
{
    public class DatabricksJob
    {
        [JsonProperty("typeName")]
        public string TypeName = "databricks_job";
        [JsonProperty("attributes")]
        public DatabricksJobAttributes Attributes = new DatabricksJobAttributes();
        [JsonProperty("relationshipAttributes")]
        public DatabricksJobRelationshipAttributes RelationshipAttributes = new DatabricksJobRelationshipAttributes();
    }

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