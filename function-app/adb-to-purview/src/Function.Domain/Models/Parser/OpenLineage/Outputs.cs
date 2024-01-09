using Newtonsoft.Json;

namespace Function.Domain.Models.OL
{
    public class Outputs : IInputsOutputs
    {
        [JsonProperty("name")]
        public string Name { get; set; } = "";
        [JsonProperty("namespace")]
        public string NameSpace { get; set; } = "";
        [JsonProperty("facets")]
        public OutputFacets Facets = new OutputFacets();

        public static Outputs CreateInstance(string item, string namespaceValue)
        {
            // TODO check mani , facets are not requied? we are not creating
            return new Outputs { Name = item, NameSpace = namespaceValue };
        }
    }
}