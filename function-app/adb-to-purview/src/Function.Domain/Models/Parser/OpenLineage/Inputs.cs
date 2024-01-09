using Newtonsoft.Json;

namespace Function.Domain.Models.OL
{
    public class Inputs: IInputsOutputs
    {
        public string Name { get; set; } = "";
        [JsonProperty("namespace")]
        public string NameSpace { get; set; } = "";

        public static Inputs CreateInstance(string item, string namespaceValue)
        {
            return new Inputs { Name = item, NameSpace = namespaceValue };
        }
    }
}