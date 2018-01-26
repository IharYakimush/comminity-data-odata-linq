namespace Community.OData.Linq.xTests
{
    using System.Collections;
    using System.Linq;

    using Community.OData.Linq.xTests.SampleData;

    using Xunit;

    public class FilterNavigationCollectionTests
    {
        [Fact]
        public void WhereCol1()
        {
            var result = ClassWithCollection.CreateQuery().OData().Filter("Link2/any(s: s/Id eq 311)").ToArray();

            Assert.Single((IEnumerable)result);
            Assert.Equal(31, result[0].Id);
        }
    }
}