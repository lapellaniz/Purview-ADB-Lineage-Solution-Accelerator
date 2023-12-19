using Function.Domain.Models.Purview;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Function.Domain.Helpers.Hash
{
    /// <summary>
    /// Represents a broker for hashing Purview asset names
    /// </summary>
    public interface IPurviewAssetNameHashBroker
    {
        /// <summary>
        /// Creates a hash for a Purview asset name given the inputs and outputs
        /// </summary>
        Task<string> CreateHashAsync(List<InputOutput> inputs, List<InputOutput> output);
    }
}