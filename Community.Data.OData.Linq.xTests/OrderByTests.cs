using System.Linq;
using Community.OData.Linq.xTests.SampleData;
using Xunit;

namespace Community.OData.Linq.xTests
{
    public class OrderByTests
    {
        [Fact]
        public void OrderByIdAsc()
        {
            var result = SimpleClass.CreateQuery().OData().OrderBy("Id asc").First();

            Assert.Equal(1, result.Id);
        }

        [Fact]
        public void OrderByIdDesc()
        {
            var result = SimpleClass.CreateQuery().Take(2).OData().OrderBy("Id desc").First();

            Assert.Equal(2, result.Id);
        }

        [Fact]
        public void OrderByIdDefault()
        {
            var result = SimpleClass.CreateQuery().OData().OrderBy("Id,Name").First();

            Assert.Equal(1, result.Id);
        }
    }
}