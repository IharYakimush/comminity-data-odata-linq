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
        }

        [Fact]
        public void ExpandLink()
        {
            ISelectExpandWrapper[] result = ClassWithLink.CreateQuery().OData().SelectExpand(null, "link1").ToArray();

            IDictionary<string, object> metadata = result[0].ToDictionary();

            // Not expanded by default
            Assert.Equal(4, metadata.Count);
        }
    }
}