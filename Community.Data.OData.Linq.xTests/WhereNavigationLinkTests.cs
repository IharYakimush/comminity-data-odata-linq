using System.Collections;
using System.Linq;
using Xunit;

namespace Community.Data.OData.Linq.Tests
{
    public class WhereNavigationLinkTests
    {
        [Fact]
        public void WhereNav1()
        {
            var result = TestClass2.CreateQuery().OData().Where("Id eq 21").ToArray();

            Assert.Single((IEnumerable) result);
            Assert.Equal(21, result[0].Id);
        }
    }
}