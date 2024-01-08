using Moq;
using Xunit;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Function.Domain.Models.OL;
using Function.Domain.Services;
using Function.Domain.Helpers;
using System.Collections.Generic;

namespace UnitTests.Function.Domain.Helpers.OlProcessing
{
    public class OlSynapseMessageConsolidationTests
    {
        private readonly Mock<ILoggerFactory> loggerFactoryMock;
        private readonly Mock<IBlobProvider> _blobProviderMock;
        private readonly OlSynapseMessageConsolidation _consolidator;

        public OlSynapseMessageConsolidationTests()
        {

            this.loggerFactoryMock = new Mock<ILoggerFactory>();
            this._blobProviderMock = new Mock<IBlobProvider>();
            this._consolidator = new OlSynapseMessageConsolidation(this.loggerFactoryMock.Object, this._blobProviderMock.Object);
        }

        [Fact]
        public async Task Consolidate_SynapseOlMessage_SingleEvent_WithSingleInputs_Output_ReturnsCountOne()
        {
            string inputBlobPath = "notebook_1701716033/Input/9277BCFB26F29242CD91BB768D4F636A20BDB534F0F9D76A4F9C6012A595C4E9";
            string outPutBlobPath = "notebook_1701716033/Output/9277BCFB26F29242CD91BB768D4F636A20BDB534F0F9D76A4F9C6012A595C4E9";

            string inputBlobJson = "{\"Name\":\"/reference/table_lkp\",\"namespace\":\"aa://namespace.dfs.core.windows.net\"}";
            string outputBlobJson = "{\"name\":\"/reference/table_lkp\",\"namespace\":\"aa://namespace.dfs.core.windows.net\"}";
            // Arrange
            this._blobProviderMock.Setup(a => a.BlobExistsAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);
            this._blobProviderMock.Setup(a => a.UploadAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));
            this._blobProviderMock.Setup(a => a.GetBlobsByHierarchyAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(new List<string> { inputBlobPath });
            this._blobProviderMock.Setup(a => a.GetBlobsByHierarchyAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(new List<string> { outPutBlobPath });
            this._blobProviderMock.Setup(a => a.DownloadBlobAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(inputBlobJson);
            this._blobProviderMock.Setup(a => a.DownloadBlobAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(outputBlobJson);

            var olPayload = "{\"eventTime\":\"2023-12-04T18:59:16.759Z\",\"eventType\":\"START\",\"run\":{\"runId\":\"8809cd34-bddd-4874-8049-988c346d92c1\"},\"job\":{\"namespace\":\"synw-udf-dlz-dv-eu2-01,azuresynapsespark\",\"name\":\"notebook_1701716033.execute_merge_into_command.reference_table_lkp\",\"facets\":{}},\"inputs\":[{\"namespace\":\"aa://namespace.dfs.core.windows.net\",\"name\":\"/reference/table_lkp\",\"inputFacets\":{}}],\"outputs\":[{\"namespace\":\"aa://namespace.dfs.core.windows.net\",\"name\":\"/reference/table_lkp\",\"outputFacets\":{}}]}";
            var olEvent = JsonConvert.DeserializeObject<Event>(olPayload);

            // Act
            Xunit.Assert.NotNull(olEvent);
            var actual = await _consolidator.ConsolidateEventAsync(olEvent, "notebook_1701716033");

            // Assert
            Xunit.Assert.Single(actual.Inputs);
            Xunit.Assert.Single(actual.Outputs);
        }

        [Fact]
        public async Task Consolidate_SynapseOlMessage_MultipleEvent_WithSameSingleInputs_Output_SameRunId_ReturnsCountOne()
        {
            string inputBlobPath = "notebook_1701716033/Input/9277BCFB26F29242CD91BB768D4F636A20BDB534F0F9D76A4F9C6012A595C4E9";
            string outPutBlobPath = "notebook_1701716033/Output/9277BCFB26F29242CD91BB768D4F636A20BDB534F0F9D76A4F9C6012A595C4E9";

            string inputBlobJson = "{\"Name\":\"/reference/table_lkp\",\"namespace\":\"aa://namespace.dfs.core.windows.net\"}";
            string outputBlobJson = "{\"name\":\"/reference/table_lkp\",\"namespace\":\"aa://namespace.dfs.core.windows.net\"}";
            // Arrange
            this._blobProviderMock.Setup(a => a.BlobExistsAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
            this._blobProviderMock.Setup(a => a.UploadAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));
            this._blobProviderMock.Setup(a => a.GetBlobsByHierarchyAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(new List<string> { inputBlobPath });
            this._blobProviderMock.Setup(a => a.GetBlobsByHierarchyAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(new List<string> { outPutBlobPath });
            this._blobProviderMock.Setup(a => a.DownloadBlobAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(inputBlobJson);
            this._blobProviderMock.Setup(a => a.DownloadBlobAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(outputBlobJson);

            var olPayload = "{\"eventTime\":\"2023-12-04T18:59:16.759Z\",\"eventType\":\"START\",\"run\":{\"runId\":\"8809cd34-bddd-4874-8049-988c346d92c1\"},\"job\":{\"namespace\":\"synw-udf-dlz-dv-eu2-01,azuresynapsespark\",\"name\":\"notebook_1701716033.execute_merge_into_command.reference_table_lkp\",\"facets\":{}},\"inputs\":[{\"namespace\":\"aa://namespace.dfs.core.windows.net\",\"name\":\"/reference/table_lkp\",\"inputFacets\":{}}],\"outputs\":[{\"namespace\":\"aa://namespace.dfs.core.windows.net\",\"name\":\"/reference/table_lkp\",\"outputFacets\":{}}]}";
            var olEvent = JsonConvert.DeserializeObject<Event>(olPayload);

            // Act
            Xunit.Assert.NotNull(olEvent);
            var actual = await _consolidator.ConsolidateEventAsync(olEvent, "notebook_1701716033");

            // Assert
            Xunit.Assert.Single(actual.Inputs);
            Xunit.Assert.Single(actual.Outputs);
        }

        [Fact]
        public async Task Consolidate_SynapseOlMessage_MultipleEvent_WithDifferentInputs_Output_SameRunId_ReturnsCountTwo()
        {
            string inputBlobPath1 = "notebook_1701716033/Input/9277BCFB26F29242CD91BB768D4F636A20BDB534F0F9D76A4F9C6012A595C4E9";
            string outPutBlobPath1 = "notebook_1701716033/Output/9277BCFB26F29242CD91BB768D4F636A20BDB534F0F9D76A4F9C6012A595C4E9";

            string inputBlobPath2 = "notebook_1701716033/Input/9277BCFB26F29242CD91BB768D4F636A20BDB534F0F9D76A4F9C6012A595C4E8";
            string outPutBlobPath2 = "notebook_1701716033/Output/9277BCFB26F29242CD91BB768D4F636A20BDB534F0F9D76A4F9C6012A595C4E8";

            string inputBlobJson1 = "{\"Name\":\"/reference/table_lkp\",\"namespace\":\"aa://namespace.dfs.core.windows.net\"}";
            string outputBlobJson1 = "{\"name\":\"/reference/table_lkp\",\"namespace\":\"aa://namespace.dfs.core.windows.net\"}";

            string inputBlobJson2 = "{\"Name\":\"/reference/table_lkpp\",\"namespace\":\"aa://namespace.dfs.core.windows.nett\"}";
            string outputBlobJson2 = "{\"name\":\"/reference/table_lkppp\",\"namespace\":\"aa://namespace.dfs.core.windows.nettt\"}";

            // Arrange
            this._blobProviderMock.Setup(a => a.BlobExistsAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);
            this._blobProviderMock.Setup(a => a.UploadAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));
            this._blobProviderMock.Setup(a => a.GetBlobsByHierarchyAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(new List<string> { inputBlobPath1 });
            this._blobProviderMock.Setup(a => a.GetBlobsByHierarchyAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(new List<string> { outPutBlobPath1 });
            this._blobProviderMock.Setup(a => a.DownloadBlobAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(inputBlobJson1);
            this._blobProviderMock.Setup(a => a.DownloadBlobAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(outputBlobJson1);

            var olPayload1 = "{\"eventTime\":\"2023-12-04T18:59:16.759Z\",\"eventType\":\"START\",\"run\":{\"runId\":\"8809cd34-bddd-4874-8049-988c346d92c1\"},\"job\":{\"namespace\":\"synw-udf-dlz-dv-eu2-01,azuresynapsespark\",\"name\":\"notebook_1701716033.execute_merge_into_command.reference_table_lkp\",\"facets\":{}},\"inputs\":[{\"namespace\":\"aa://namespace.dfs.core.windows.nett\",\"name\":\"/reference/table_lkpp\",\"inputFacets\":{}}],\"outputs\":[{\"namespace\":\"aa://namespace.dfs.core.windows.nettt\",\"name\":\"/reference/table_lkppp\",\"outputFacets\":{}}]}";
            var olEvent1 = JsonConvert.DeserializeObject<Event>(olPayload1);

            this._blobProviderMock.Setup(a => a.BlobExistsAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);
            this._blobProviderMock.Setup(a => a.UploadAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));
            this._blobProviderMock.Setup(a => a.GetBlobsByHierarchyAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(new List<string> { inputBlobPath1, inputBlobPath2 });
            this._blobProviderMock.Setup(a => a.GetBlobsByHierarchyAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(new List<string> { outPutBlobPath1, outPutBlobPath2 });
            this._blobProviderMock.Setup(a => a.DownloadBlobAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(inputBlobJson1);
            this._blobProviderMock.Setup(a => a.DownloadBlobAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(outputBlobJson1);
            this._blobProviderMock.Setup(a => a.DownloadBlobAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(inputBlobJson2);
            this._blobProviderMock.Setup(a => a.DownloadBlobAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(outputBlobJson2);

            var olPayload2 = "{\"eventTime\":\"2023-12-04T18:59:16.759Z\",\"eventType\":\"START\",\"run\":{\"runId\":\"8809cd34-bddd-4874-8049-988c346d92c1\"},\"job\":{\"namespace\":\"synw-udf-dlz-dv-eu2-01,azuresynapsespark\",\"name\":\"notebook_1701716033.execute_merge_into_command.reference_table_lkp\",\"facets\":{}},\"inputs\":[{\"namespace\":\"aa://namespace.dfs.core.windows.nett\",\"name\":\"/reference/table_lkpp\",\"inputFacets\":{}}],\"outputs\":[{\"namespace\":\"aa://namespace.dfs.core.windows.nettt\",\"name\":\"/reference/table_lkppp\",\"outputFacets\":{}}]}";
            var olEvent2 = JsonConvert.DeserializeObject<Event>(olPayload2);

            // Act
            Xunit.Assert.NotNull(olEvent1);
            Xunit.Assert.NotNull(olEvent2);
            await _consolidator.ConsolidateEventAsync(olEvent1, "notebook_1701716033");
            var actual = await _consolidator.ConsolidateEventAsync(olEvent2, "notebook_1701716033");

            // Assert
            Xunit.Assert.Equal(2, actual.Inputs.Count);
            Xunit.Assert.Equal(2, actual.Outputs.Count);
            // modify the names , notebooks , etc
        }
    }
}