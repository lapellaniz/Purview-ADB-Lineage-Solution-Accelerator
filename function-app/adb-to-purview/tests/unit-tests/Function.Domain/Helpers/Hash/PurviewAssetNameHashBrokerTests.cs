using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Function.Domain.Helpers.Hash;
using Function.Domain.Models;
using Function.Domain.Models.Purview;
using Xunit;

namespace unit_tests.Function.Domain.Helpers.Hash
{
    public class PurviewAssetNameHashBrokerTests
    {
        private readonly IPurviewAssetNameHashBroker _purviewAssetNameHashBroker;

        public PurviewAssetNameHashBrokerTests()
        {
            _purviewAssetNameHashBroker = new PurviewAssetNameHashBroker();
        }

        [Fact]
        public async Task Given_Data_Expect_HashValid()
        {
            // Arrange
            InputOutput input = new()
            {
                UniqueAttributes = new UniqueAttributes()
                {
                    QualifiedName = "test-input"
                }
            };
            List<InputOutput> inputs = [input];
            InputOutput output = new()
            {
                UniqueAttributes = new UniqueAttributes()
                {
                    QualifiedName = "test-output"
                }
            };
            List<InputOutput> outputs = [output];

            // Act
            var actual = await _purviewAssetNameHashBroker.CreateHashAsync(inputs, outputs);

            // Assert
            Assert.NotNull(actual);
            Assert.NotEmpty(actual);
            Assert.Equal("b5f7f2b3e491718deb69195be3284b1b->45fe4506e05c5b111ba406dd35388141", actual);
        }

        [Fact]
        public async Task Given_Data_Expect_HashNotNullOrEmpty()
        {
            // Arrange
            InputOutput input = new()
            {
                UniqueAttributes = new UniqueAttributes()
                {
                    QualifiedName = "test-input"
                }
            };
            List<InputOutput> inputs = [input];
            InputOutput output = new()
            {
                UniqueAttributes = new UniqueAttributes()
                {
                    QualifiedName = "test-output"
                }
            };
            List<InputOutput> outputs = [output];

            // Act
            var actual = await _purviewAssetNameHashBroker.CreateHashAsync(inputs, outputs);

            // Assert
            Assert.NotNull(actual);
            Assert.NotEmpty(actual);
        }
    }
}