using Newtonsoft.Json;
using System.Collections.Generic;
namespace Function.Domain.Models.Purview
{
    public class DatabricksWorkspace
    {
        [JsonProperty("typeName")]
        public string TypeName = "databricks_workspace";
        [JsonProperty("attributes")]
        public BaseAttributes Attributes = new BaseAttributes();
        [JsonProperty("relationshipAttributes")]
        public Dictionary<string,string> RelationshipAttributes = new Dictionary<string,string>();
    }

     public class SynapseWorkspace
    {
        [JsonProperty("typeName")]
        public string TypeName = "azure_synapse_workspace";
        [JsonProperty("attributes")]
        public BaseAttributes Attributes = new BaseAttributes();
        [JsonProperty("relationshipAttributes")]
        public Dictionary<string,string> RelationshipAttributes = new Dictionary<string,string>();
    }
}