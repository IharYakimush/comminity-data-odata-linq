using System.Linq;
using Community.OData.Linq.xTests.SampleData;
using Microsoft.OData;
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

        [Fact]
        public void OrderByIdCaseInsensitiveDefault()
        {
            var result = SimpleClass.CreateQuery().OData().OrderBy("id desc,name").First();

            Assert.Equal(2, result.Id);
        }

        [Fact]
        public void OrderByIdCaseSensitiveConfig()
        {
            Assert.Throws<ODataException>(
                () => SimpleClass.CreateQuery().OData(s => s.EnableCaseInsensitive = false).OrderBy("id desc,name"));
        }


        [Fact]
        public void OrderByNotSortable()
        {
            Assert.Throws<ODataException>(() =>
                SimpleClass.CreateQuery().OData().OrderBy($"{nameof(SimpleClass.NotOrderable)}"));
        }
    }
}