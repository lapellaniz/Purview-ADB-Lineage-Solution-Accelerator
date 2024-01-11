using Moq;
using Xunit;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Function.Domain.Models.OL;
using Function.Domain.Services;
using Function.Domain.Helpers;
using System.Collections.Generic;
using Function.Domain.Providers;

namespace UnitTests.Function.Domain.Helpers.OlProcessing
{
    public class OlSynapseMessageEnrichmentTests
    {
        private readonly Mock<ILoggerFactory> loggerFactoryMock;
        private readonly Mock<ISynapseClientProvider> _synapseClientProviderMock;
        private readonly OlSynapseMessageEnrichment _consolidator;

        public OlSynapseMessageEnrichmentTests()
        {

            this.loggerFactoryMock = new Mock<ILoggerFactory>();
            this._synapseClientProviderMock = new Mock<ISynapseClientProvider>();
            this._consolidator = new OlSynapseMessageEnrichment(this.loggerFactoryMock.Object, this._synapseClientProviderMock.Object);
        }

        [Fact]
        public async Task Enrichment_SynapseOlMessage_SingleEvent_AfterEnrichment_Inputs_Outputs_Increment()
        {
            string location = "ab://testsynapse1.dfs.core.windows.net";
            // Arrange
            this._synapseClientProviderMock.Setup(a => a.GetSynapseStorageLocation(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(location);

            var olPayload = "{\"eventTime\":\"2023-12-04T18:59:16.759Z\",\"eventType\":\"START\",\"run\":{\"runId\":\"8809cd34-bddd-4874-8049-988c346d92c1\",\"facets\":{\"spark.logicalPlan\":{\"plan\":[{\"class\":\"org.apache.spark.sql.delta.commands.MergeIntoCommand\",\"num-children\":0,\"source\":[{\"class\":\"org.apache.spark.sql.execution.datasources.LogicalRelation\",\"catalogTable\":{\"product-class\":\"org.apache.spark.sql.catalyst.catalog.CatalogTable\",\"identifier\":{\"product-class\":\"org.apache.spark.sql.catalyst.TableIdentifier\",\"table\":\"loinc_codes_current\",\"database\":\"raw\"}},\"isStreaming\":false}],\"target\":[{\"class\":\"org.apache.spark.sql.execution.datasources.LogicalRelation\",\"catalogTable\":{\"product-class\":\"org.apache.spark.sql.catalyst.catalog.CatalogTable\",\"identifier\":{\"product-class\":\"org.apache.spark.sql.catalyst.TableIdentifier\",\"table\":\"ref_mstr_lkp\",\"database\":\"reference\"}},\"isStreaming\":false}],\"targetFileIndex\":null}]}}},\"job\":{\"namespace\":\"synw-udf-dlz-dv-eu2-01,azuresynapsespark\",\"name\":\"n_b_1701716033.execute_merge_into_command.reference_ref_mstr_lkp\",\"facets\":{}},\"inputs\":[{\"namespace\":\"abfss://abc.core.windows.net\",\"name\":\"/reference/ref_mstr_lkp\",\"inputFacets\":{}},{\"namespace\":\"ab://testsynapse.dfs.core.windows.net\",\"name\":\"/raw/loinc_codes_current\",\"inputFacets\":{}}],\"outputs\":[{\"namespace\":\"abfss://abc.core.windows.net\",\"name\":\"/reference/ref_mstr_lkp\",\"outputFacets\":{}},{\"namespace\":\"ab://testsynapse.dfs.core.windows.net\",\"name\":\"/reference/ref_mstr_lkp\",\"outputFacets\":{}}]}";
            var olEvent = JsonConvert.DeserializeObject<Event>(olPayload);

            // Act
            Xunit.Assert.NotNull(olEvent);
            var inputCount = olEvent.Inputs.Count;
            var outPutCount = olEvent.Outputs.Count;
            var actual = await _consolidator.EnrichmentEventAsync(olEvent, "n_b_1701716033");

            // Assert
            Xunit.Assert.True(actual.Inputs.Count > inputCount);
            Xunit.Assert.True(actual.Outputs.Count > outPutCount);
        }

        [Fact]
        public async Task Enrichment_SynapseOlMessage_SingleEvent_AfterEnrichment_Inputs_Increment()
        {
            string location = "ab://testsynapse1.dfs.core.windows.net";
            // Arrange
            this._synapseClientProviderMock.Setup(a => a.GetSynapseStorageLocation(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(location);

            var olPayload = "{\"eventTime\":\"2023-12-04T18:59:16.759Z\",\"eventType\":\"START\",\"run\":{\"runId\":\"8809cd34-bddd-4874-8049-988c346d92c1\",\"facets\":{\"spark.logicalPlan\":{\"plan\":[{\"class\":\"org.apache.spark.sql.delta.commands.MergeIntoCommand\",\"num-children\":0,\"source\":[{\"class\":\"org.apache.spark.sql.execution.datasources.LogicalRelation\",\"catalogTable\":{\"product-class\":\"org.apache.spark.sql.catalyst.catalog.CatalogTable\",\"identifier\":{\"product-class\":\"org.apache.spark.sql.catalyst.TableIdentifier\",\"table\":\"loinc_codes_current\",\"database\":\"raw\"}},\"isStreaming\":false}],\"target\":[{\"class\":\"org.apache.spark.sql.execution.datasources.LogicalRelation\",\"catalogTable\":{\"product-class\":\"org.apache.spark.sql.catalyst.catalog.CatalogTable\",\"identifier\":{\"product-class\":\"org.apache.spark.sql.catalyst.TableIdentifier\",\"table\":\"ref_mstr_lkp\",\"database\":\"reference\"}},\"isStreaming\":false}],\"targetFileIndex\":null}]}}},\"job\":{\"namespace\":\"synw-udf-dlz-dv-eu2-01,azuresynapsespark\",\"name\":\"n_b_1701716033.execute_merge_into_command.reference_ref_mstr_lkp\",\"facets\":{}},\"inputs\":[{\"namespace\":\"abfss://abc.core.windows.net\",\"name\":\"/reference/ref_mstr_lkp\",\"inputFacets\":{}},{\"namespace\":\"ab://testsynapse.dfs.core.windows.net\",\"name\":\"/raw/loinc_codes_current\",\"inputFacets\":{}}]}";
            var olEvent = JsonConvert.DeserializeObject<Event>(olPayload);

            // Act
            Xunit.Assert.NotNull(olEvent);
            var inputCount = olEvent.Inputs.Count;
            var actual = await _consolidator.EnrichmentEventAsync(olEvent, "n_b_1701716033");

            // Assert
            Xunit.Assert.True(actual.Inputs.Count > inputCount);
        }

        [Fact]
        public async Task Enrichment_SynapseOlMessage_SingleEvent_AfterEnrichment_Outputs_Increment()
        {
            string location = "ab://testsynapse1.dfs.core.windows.net";
            // Arrange
            this._synapseClientProviderMock.Setup(a => a.GetSynapseStorageLocation(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(location);

            var olPayload = "{\r\n  \"eventTime\": \"2023-12-04T18:59:16.759Z\",\r\n  \"eventType\": \"START\",\r\n  \"run\": {\r\n    \"runId\": \"8809cd34-bddd-4874-8049-988c346d92c1\",\r\n    \"facets\": {\r\n      \"spark.logicalPlan\": {\r\n        \"plan\": [\r\n          {\r\n            \"class\": \"org.apache.spark.sql.delta.commands.MergeIntoCommand\",\r\n            \"num-children\": 0,\r\n            \"source\": [\r\n              {\r\n                \"class\": \"org.apache.spark.sql.execution.datasources.LogicalRelation\",\r\n                \"catalogTable\": {\r\n                  \"product-class\": \"org.apache.spark.sql.catalyst.catalog.CatalogTable\",\r\n                  \"identifier\": {\r\n                    \"product-class\": \"org.apache.spark.sql.catalyst.TableIdentifier\",\r\n                    \"table\": \"loinc_codes_current\",\r\n                    \"database\": \"raw\"\r\n                  }\r\n                },\r\n                \"isStreaming\": false\r\n              }\r\n            ],\r\n            \"target\": [\r\n              {\r\n                \"class\": \"org.apache.spark.sql.execution.datasources.LogicalRelation\",\r\n                \"catalogTable\": {\r\n                  \"product-class\": \"org.apache.spark.sql.catalyst.catalog.CatalogTable\",\r\n                  \"identifier\": {\r\n                    \"product-class\": \"org.apache.spark.sql.catalyst.TableIdentifier\",\r\n                    \"table\": \"ref_mstr_lkp\",\r\n                    \"database\": \"reference\"\r\n                  }\r\n                },\r\n                \"isStreaming\": false\r\n              }\r\n            ],\r\n            \"targetFileIndex\": null\r\n          }\r\n        ]\r\n      }\r\n    }\r\n  },\r\n  \"job\": {\r\n    \"namespace\": \"synw-udf-dlz-dv-eu2-01,azuresynapsespark\",\r\n    \"name\": \"n_b_1701716033.execute_merge_into_command.reference_ref_mstr_lkp\",\r\n    \"facets\": {}\r\n  },\r\n  \"outputs\": [\r\n    {\r\n      \"namespace\": \"abfss://abc.core.windows.net\",\r\n      \"name\": \"/reference/ref_mstr_lkp\",\r\n      \"outputFacets\": {}\r\n    },\r\n    {\r\n      \"namespace\": \"ab://testsynapse.dfs.core.windows.net\",\r\n      \"name\": \"/reference/ref_mstr_lkp\",\r\n      \"outputFacets\": {}\r\n    }\r\n  ]\r\n}";
            var olEvent = JsonConvert.DeserializeObject<Event>(olPayload);

            // Act
            Xunit.Assert.NotNull(olEvent);
            var outPutCount = olEvent.Outputs.Count;
            var actual = await _consolidator.EnrichmentEventAsync(olEvent, "n_b_1701716033");

            // Assert
            Xunit.Assert.True(actual.Outputs.Count > outPutCount);
        }

        [Fact]
        public async Task Enrichment_SynapseOlMessage_SingleEvent_AfterEnrichment_Same_Inputs_Outputs()
        {
            string location = "ab://testsynapse.dfs.core.windows.net";
            // Arrange
            this._synapseClientProviderMock.Setup(a => a.GetSynapseStorageLocation(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(location);

            var olPayload = "{\"eventTime\":\"2023-12-04T18:59:16.759Z\",\"eventType\":\"START\",\"run\":{\"runId\":\"8809cd34-bddd-4874-8049-988c346d92c1\",\"facets\":{\"spark.logicalPlan\":{\"plan\":[{\"class\":\"org.apache.spark.sql.delta.commands.MergeIntoCommand\",\"num-children\":0,\"source\":[{\"class\":\"org.apache.spark.sql.execution.datasources.LogicalRelation\",\"catalogTable\":{\"product-class\":\"org.apache.spark.sql.catalyst.catalog.CatalogTable\",\"identifier\":{\"product-class\":\"org.apache.spark.sql.catalyst.TableIdentifier\",\"table\":\"loinc_codes_current\",\"database\":\"raw\"}},\"isStreaming\":false}],\"target\":[{\"class\":\"org.apache.spark.sql.execution.datasources.LogicalRelation\",\"catalogTable\":{\"product-class\":\"org.apache.spark.sql.catalyst.catalog.CatalogTable\",\"identifier\":{\"product-class\":\"org.apache.spark.sql.catalyst.TableIdentifier\",\"table\":\"ref_mstr_lkp\",\"database\":\"reference\"}},\"isStreaming\":false}],\"targetFileIndex\":null}]}}},\"job\":{\"namespace\":\"synw-udf-dlz-dv-eu2-01,azuresynapsespark\",\"name\":\"n_b_1701716033.execute_merge_into_command.reference_ref_mstr_lkp\",\"facets\":{}},\"inputs\":[{\"namespace\":\"abfss://abc.core.windows.net\",\"name\":\"/reference/ref_mstr_lkp\",\"inputFacets\":{}},{\"namespace\":\"ab://testsynapse.dfs.core.windows.net\",\"name\":\"/raw/loinc_codes_current\",\"inputFacets\":{}}],\"outputs\":[{\"namespace\":\"abfss://abc.core.windows.net\",\"name\":\"/reference/ref_mstr_lkp\",\"outputFacets\":{}},{\"namespace\":\"ab://testsynapse.dfs.core.windows.net\",\"name\":\"/reference/ref_mstr_lkp\",\"outputFacets\":{}}]}";
            var olEvent = JsonConvert.DeserializeObject<Event>(olPayload);

            // Act
            Xunit.Assert.NotNull(olEvent);
            var actual = await _consolidator.EnrichmentEventAsync(olEvent, "n_b_1701716033");

            // Assert
            Xunit.Assert.Equal(olEvent.Inputs.Count, actual.Inputs.Count);
            Xunit.Assert.Equal(olEvent.Outputs.Count, actual.Outputs.Count);
        }


    }
}