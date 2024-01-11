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
                List<IInputsOutputs> inputs = await CaptureNameSpaceAsync<Inputs>(sourceNames, workspaceName);
                List<IInputsOutputs> outputs = await CaptureNameSpaceAsync<Outputs>(targetNames, workspaceName);

                // Merge and create a distinct collection based on Name and NameSpace
                if (inputs.Count > 0)
                {
                    olEvent.Inputs = MergeAndDistinct(olEvent.Inputs.Cast<IInputsOutputs>().ToList(), inputs, x => x.Name, x => x.NameSpace).OfType<Inputs>().ToList();
                }

                if (outputs.Count > 0)
                {
                    olEvent.Outputs = MergeAndDistinct(olEvent.Outputs.Cast<IInputsOutputs>().ToList(), outputs, x => x.Name, x => x.NameSpace).OfType<Outputs>().ToList();
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "OlSynapseMessageEnrichment-EnrichmentEventAsync: ErrorMessage {ErrorMessage} ", ex.Message);
            }
            return olEvent;
        }

        private async Task<List<IInputsOutputs>> CaptureNameSpaceAsync<T>(HashSet<string> names, string workspaceName)
            where T : IInputsOutputs, new()
        {
            List<IInputsOutputs> result = [];
            foreach (var item in names)
            {
                // The item is in the format of /database/table
                var values = item.TrimStart('/').Split('/');
                var databaseName = values[0].Trim();
                var tableName = values[1].Trim();
                var nameSpace = await _synapseClientProvider.GetSynapseStorageLocation(workspaceName, databaseName, tableName);
                if (!string.IsNullOrEmpty(nameSpace))
                {
                    result.Add(new T() { Name = item, NameSpace = nameSpace });
                }
                else
                {
                    _log.LogWarning("OlSynapseMessageEnrichment-CaptureNameSpaceAsync: Issue with bearer token or storage location not exist for database : {databaseName} and table : {tableName}", databaseName, tableName);
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
                    string table = logicalRelation["catalogTable"]["identifier"]["table"]?.ToString().Trim();
                    string database = logicalRelation["catalogTable"]["identifier"]["database"]?.ToString().Trim();

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

        private static List<IInputsOutputs> MergeAndDistinct(List<IInputsOutputs> existingList, List<IInputsOutputs> newList, params Func<IInputsOutputs, object>[] keySelectors)
        {
            return existingList
                .UnionBy(newList, item => string.Join("_", keySelectors.Select(selector => selector(item))))
                .ToList();
        }
    }
}