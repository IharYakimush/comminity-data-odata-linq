namespace Community.OData.Linq.xTests
{
    using System.Collections;
    using System.Linq;

    using Community.OData.Linq.xTests.SampleData;

    using Xunit;

    public class FilterNavigationLinkTests
    {
        [Fact]
        public void WhereNav1()
        {                        
            var result = ClassWithLink.CreateQuery().OData().Filter("Link1/Id eq 211").ToArray();

            Assert.Single((IEnumerable) result);
            Assert.Equal(21, result[0].Id);
        }
    }
}