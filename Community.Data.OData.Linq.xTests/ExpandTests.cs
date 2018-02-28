namespace Community.OData.Linq.xTests
{
    using System.Collections.Generic;
    using System.Linq;

    using Community.OData.Linq.xTests.SampleData;
    using Microsoft.OData;
    using Xunit;

    public class ExpandTests
    {
        [Fact]
        public void DefaultExpand()
        {
            ISelectExpandWrapper[] result = ClassWithLink.CreateQuery().OData().SelectExpand().ToArray();

            IDictionary<string, object> metadata = result[0].ToDictionary();

            // Not expanded by default except auto expand attribute
            Assert.Equal(3, metadata.Count);
            Assert.Null(metadata["Link3"]);
        }

        [Fact]
        public void ExpandLink()
        {
            ISelectExpandWrapper[] result = ClassWithLink.CreateQuery().OData().SelectExpand(null, "Link1").ToArray();

            IDictionary<string, object> metadata = result[0].ToDictionary();

            // Not expanded by default
            Assert.Equal(4, metadata.Count);

            Assert.Equal(6, (metadata["Link1"] as ISelectExpandWrapper).ToDictionary().Count);
        }

        [Fact]
        public void ExpandSelect()
        {
            ISelectExpandWrapper[] result = ClassWithLink.CreateQuery().OData().SelectExpand("Name,Link1", "Link1").ToArray();

            IDictionary<string, object> metadata = result[0].ToDictionary();

            // Not expanded by default
            Assert.Equal(3, metadata.Count);
            Assert.Equal(6, (metadata["Link1"] as ISelectExpandWrapper).ToDictionary().Count);
        }

        [Fact]
        public void ExpandLinkSelect()
        {
            ISelectExpandWrapper[] result = ClassWithLink.CreateQuery().OData().SelectExpand("Name", "Link1($select=Name)").ToArray();

            IDictionary<string, object> metadata = result[0].ToDictionary();

            // Not expanded by default
            Assert.Equal(3, metadata.Count);
            Assert.Equal(1, (metadata["Link1"] as ISelectExpandWrapper).ToDictionary().Count);
        }

        [Fact]
        public void ExpandCollection()
        {
            ISelectExpandWrapper[] result = ClassWithCollection.CreateQuery().OData().SelectExpand("Name", "Link2").ToArray();

            IDictionary<string, object> metadata = result[0].ToDictionary();

            // Not expanded by default
            Assert.Equal(2, metadata.Count);
            Assert.Equal(2, (metadata["Link2"] as IEnumerable<ISelectExpandWrapper>).Count());
        }

        [Fact]
        public void ExpandCollectionWithTop()
        {
            ISelectExpandWrapper[] result = ClassWithCollection.CreateQuery().OData().SelectExpand("Name", "Link2($top=1)").ToArray();

            IDictionary<string, object> metadata = result[0].ToDictionary();

            // Not expanded by default
            Assert.Equal(2, metadata.Count);
            Assert.Single((metadata["Link2"] as IEnumerable<ISelectExpandWrapper>));
        }

        [Fact]
        public void ExpandCollectionWithTopDefaultPageSize()
        {
            ISelectExpandWrapper[] result = ClassWithCollection.CreateQuery().OData(s => s.QuerySettings.PageSize = 1).SelectExpand("Name", "Link2").ToArray();

            IDictionary<string, object> metadata = result[0].ToDictionary();

            // Not expanded by default
            Assert.Equal(2, metadata.Count);
            Assert.Single((metadata["Link2"] as IEnumerable<ISelectExpandWrapper>));
        }

        [Fact]
        public void ExpandCollectionWithTop21()
        {
            ISelectExpandWrapper[] result = ClassWithCollection.CreateQuery().OData().SelectExpand("Name", "Link2($top=21)").ToArray();

            IDictionary<string, object> metadata = result[0].ToDictionary();

            // Not expanded by default
            Assert.Equal(2, metadata.Count);
            Assert.Equal(2, (metadata["Link2"] as IEnumerable<ISelectExpandWrapper>).Count());
        }

        [Fact]
        public void ExpandCollectionWithTopExceedLimit()
        {
            Assert.Throws<ODataException>(
               () => ClassWithCollection.CreateQuery().OData().SelectExpand("Name", "Link2($top=101)"));            
        }

        [Fact]
        public void ExpandCollectionWithFilterAndSelect()
        {
            ISelectExpandWrapper[] result = ClassWithCollection.CreateQuery().OData().SelectExpand("Name", "Link2($filter=Id eq 311;$select=Name)").ToArray();

            IDictionary<string, object> metadata = result[0].ToDictionary();

            // Not expanded by default
            Assert.Equal(2, metadata.Count);
            IEnumerable<ISelectExpandWrapper> collection = metadata["Link2"] as IEnumerable<ISelectExpandWrapper>;
            Assert.Single(collection);

            Assert.Equal(1, collection.Single().ToDictionary().Count);
        }
    }
}