using Function.Domain.Models.Purview;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;


namespace Function.Domain.Helpers.Hash
{
    public class PurviewAssetNameHashBroker : IPurviewAssetNameHashBroker
    {
        /// <summary>
        /// Creates a hash for a Purview asset name given the inputs and outputs
        /// </summary>
        public async Task<string> CreateHashAsync(List<InputOutput> inputs, List<InputOutput> outputs)
        {
            var inputHash = await CreateHash(inputs);
            var outputHash = await CreateHash(outputs);

            return $"{inputHash}->{outputHash}";
        }

        /// <summary>
        /// Creates a hash by ordering all items by their qualified name and then concatenating them together using a colon delimiter then hashing the result.
        /// </summary>
        private static async Task<string> CreateHash(List<InputOutput> data)
        {
            var uniqueData = data.OrderBy(x => x.UniqueAttributes.QualifiedName)
                        .Aggregate(new StringBuilder(), (sb, item) => sb.AppendFormat("{0},", item.UniqueAttributes.QualifiedName.ToLower()))
                        .ToString().TrimEnd(',');
            var ms = new MemoryStream(ASCIIEncoding.ASCII.GetBytes(uniqueData));
            var hashData = await MD5.HashDataAsync(ms);            
            return string.Concat(hashData.Select(b => b.ToString("x2")));
        }
    }
}