using Newtonsoft.Json;
namespace Function.Domain.Models.Purview
{
    public class SynapseProcessRelationshipAttributes
    {
        [JsonProperty("synapse_notebook")]
        public RelationshipAttribute Notebook = new RelationshipAttribute();
    }
}