using Newtonsoft.Json;
using System.Collections.Generic;
namespace Function.Domain.Models.Purview
{
    public class SynapseProcess
    {
        [JsonProperty("typeName")]
        public string TypeName = "azure_synapse_process";
        [JsonProperty("attributes")]
        public SynapseProcessAttributes Attributes = new SynapseProcessAttributes();
        [JsonProperty("relationshipAttributes")]
        public SynapseProcessRelationshipAttributes RelationshipAttributes = new SynapseProcessRelationshipAttributes();
        [JsonProperty("columnAttributes")]
        public List<ColumnLevelAttributes> ColumnLevel = new List<ColumnLevelAttributes>();
    }
}