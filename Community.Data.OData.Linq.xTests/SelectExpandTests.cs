namespace Community.OData.Linq.xTests
{
    using System.Collections.Generic;

    using Xunit;
    using System.Linq;

    using Community.OData.Linq.OData.Query.Expressions;
    using Community.OData.Linq.xTests.SampleData;

    public class SelectExpandTests
    {
        [Fact]
        public void SelectName()
        {
            ISelectExpandWrapper[] result = SimpleClass.CreateQuery().OData().SelectExpand("Name").ToArray();

            IDictionary<string, object> metadata = result[0].ToDictionary();

            // Expect Name to be selected
            Assert.Equal(1, metadata.Count);
            Assert.Equal("Name", metadata.Single().Key);
            Assert.Equal("n1", metadata.Single().Value);
            Assert.IsType<string>(metadata.Single().Value);
        }

        [Fact]
        public void SelectId()
        {
            ISelectExpandWrapper[] result = SimpleClass.CreateQuery().OData().SelectExpand("Id").ToArray();

            IDictionary<string, object> metadata = result[0].ToDictionary();

            Assert.Equal(1, metadata.Count);
            Assert.Equal("Id", metadata.Single().Key);
            Assert.IsType<int>(metadata.Single().Value);
            Assert.Equal(1, metadata.Single().Value);
        }

        [Fact]
        public void SelectIdCaseInsensitive()
        {
            ISelectExpandWrapper[] result = SimpleClass.CreateQuery().OData().SelectExpand("id").ToArray();

            IDictionary<string, object> metadata = result[0].ToDictionary();

            Assert.Equal(1, metadata.Count);
            Assert.Equal("Id", metadata.Single().Key);
            Assert.IsType<int>(metadata.Single().Value);
            Assert.Equal(1, metadata.Single().Value);
        }
    }
}