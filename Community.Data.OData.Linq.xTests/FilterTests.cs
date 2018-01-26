namespace Community.OData.Linq.xTests
{
    using System.Linq;

    using Community.OData.Linq.xTests.SampleData;

    using Xunit;

    public class FilterTests
    {
        [Fact]
        public void WhereById()
        {
            var result = SimpleClass.CreateQuery().OData().Filter("Id eq 1").ToArray();
            
            Assert.Single(result);
            Assert.Equal(1, result[0].Id);
        }

        [Fact]
        public void WhereByName()
        {
            var result = SimpleClass.CreateQuery().OData().Filter("Name eq 'n1'").ToArray();

            Assert.Single(result);
            Assert.Equal("n1", result[0].Name);            
        }

        [Fact]
        public void WhereByNameCaseInsensitiveKeyByDefault()
        {
            var result = SimpleClass.CreateQuery().OData().Filter("name eq 'n1'").ToArray();

            Assert.Single(result);
            Assert.Equal("n1", result[0].Name);
        }

        [Fact]
        public void WhereByDateTime()
        {
            var result = SimpleClass.CreateQuery().OData().Filter("DateTime gt 2010-01-25T02:13:40.00Z").ToArray();

            Assert.Single(result);
            Assert.Equal("n1", result[0].Name);
        }

        [Fact]
        public void WhereByEnumString()
        {
            var result = SimpleClass.CreateQuery().OData().Filter("TestEnum eq 'Item2'").ToArray();

            Assert.Single(result);
            Assert.Equal("n2", result[0].Name);
        }

        [Fact]
        public void WhereByEnumNumber()
        {
            var result = SimpleClass.CreateQuery()
                .OData()
                .Filter("TestEnum eq '2'").ToArray();

            Assert.Single(result);
            Assert.Equal("n2", result[0].Name);
        }

        [Fact]
        public void WhereByEnumStringCaseInsensitiveValueByDefault()
        {
            var result = SimpleClass.CreateQuery().OData().Filter("TestEnum eq 'item2'").ToArray();

            Assert.Single(result);
            Assert.Equal("n2", result[0].Name);
        }

        [Fact]
        public void WhereByEnumStringCaseInsensitiveKeyByDefault()
        {
            var result = SimpleClass.CreateQuery().OData().Filter("testEnum eq 'item2'").ToArray();

            Assert.Single(result);
            Assert.Equal("n2", result[0].Name);
        }
    }
}