using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Function.Domain.Models.OL;
using Function.Domain.Providers;
using Microsoft.Extensions.Logging;

namespace Function.Domain.Helpers
{
    public class OlSynapseMessageEnrichment : IOlMessageEnrichment
    {
        private ILogger _log;
        private const string PLAN_CLASS = "org.apache.spark.sql.delta.commands.MergeIntoCommand";
        private const string SOURCE_CLASS = "org.apache.spark.sql.execution.datasources.LogicalRelation";
        private ISynapseClientProvider _synapseClientProvider;

        public OlSynapseMessageEnrichment(ILoggerFactory loggerFactory, ISynapseClientProvider synapseClientProvider)
        {
            _log = loggerFactory.CreateLogger<OlSynapseMessageEnrichment>();
            _synapseClientProvider = synapseClientProvider;
        }

        public async Task<Event?> EnrichmentEventAsync(Event olEvent, string workspaceName)
        {
            try
            {
                // Capture Name
                var sourceNames = CaptureName(olEvent, "source");
                var targetNames = CaptureName(olEvent, "target");

                // Capture Namespace
                List<Inputs> inputs = await CaptureNameSpaceAsync<Inputs>(sourceNames, workspaceName);
                List<Outputs> outputs = await CaptureNameSpaceAsync<Outputs>(targetNames, workspaceName);

                // Merge and create a distinct collection based on Name and NameSpace
                if (inputs.Count > 0)
                {
                    olEvent.Inputs = MergeAndDistinct(olEvent.Inputs, inputs, x => x.Name, x => x.NameSpace);
                }

                if (outputs.Count > 0)
                {
                    olEvent.Outputs = MergeAndDistinct(olEvent.Outputs, outputs, x => x.Name, x => x.NameSpace);
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "OlSynapseMessageEnrichment-EnrichmentEventAsync: ErrorMessage {ErrorMessage} ", ex.Message);
            }
            return olEvent;
        }

        private async Task<List<T>> CaptureNameSpaceAsync<T>(HashSet<string> names, string workspaceName)
        {
            List<T> result = new List<T>();
            // Define a delegate for the factory method
            Func<string, string, T> factoryMethod = (Func<string, string, T>)Delegate.CreateDelegate(typeof(Func<string, string, T>), typeof(T).GetMethod("CreateInstance"));
            foreach (var item in names)
            {
                var values = item.Split('/');
                var nameSpace = await _synapseClientProvider.GetSynapseStorageLocation(workspaceName, values[1], values[2]);
                if (!string.IsNullOrEmpty(nameSpace))
                {
                    T instance = factoryMethod(item, nameSpace);
                    result.Add(instance);
                }
                else
                {
                    _log.LogWarning($"OlSynapseMessageEnrichment-CaptureNameSpaceAsync: Issue with bearer token or storage location not exist for :  {item}");
                }
            }
            return result;
        }

        private HashSet<string> CaptureName(Event olEvent, string type)
        {
            var sparkPlan = olEvent.Run.Facets.SparkLogicalPlan;
            // check for the uniqueness
            HashSet<string> uniqueTableDatabaseNames = new HashSet<string>();
            var logicalRelations = sparkPlan.SelectTokens("$.plan[?(@.class == '" + PLAN_CLASS + "')]." + type + "[?(@.class == '" + SOURCE_CLASS + "')]");
            foreach (var logicalRelation in logicalRelations)
            {
                // Check for null or empty array
                if (logicalRelation?["catalogTable"]?["identifier"] != null)
                {
                    string table = logicalRelation["catalogTable"]["identifier"]["table"]?.ToString();
                    string database = logicalRelation["catalogTable"]["identifier"]["database"]?.ToString();

                    // Check if the table and database are not null or empty
                    if (!string.IsNullOrEmpty(table) && !string.IsNullOrEmpty(database))
                    {
                        string uniqueIdentifier = $"/{database}/{table}";
                        uniqueTableDatabaseNames.Add(uniqueIdentifier);
                    }

                }
            }
            return uniqueTableDatabaseNames;
        }

        private static List<T> MergeAndDistinct<T>(List<T> existingList, List<T> newList, params Func<T, object>[] keySelectors)
         where T : class
        {
            // TO DO mani check - facets are coming , its getting added
            return existingList
                .Union(newList)
                .ToList();
        }
    }
}