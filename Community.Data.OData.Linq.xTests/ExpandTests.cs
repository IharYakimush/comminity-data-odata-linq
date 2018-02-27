namespace Community.OData.Linq.xTests
{
    using System.Collections.Generic;
    using System.Linq;

    using Community.OData.Linq.xTests.SampleData;

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
    }
}