namespace Community.Data.OData.Linq.Tests
{
    using System.Linq;
    using Xunit;


    public class ODataLinqExtensionsTests
    {
        [Fact]
        public void WhereSimple1()
        {
            var result = TestClass.CreateQuery().OData().Where("Id eq 1").ToArray();
            
            Assert.Single(result);
            Assert.Equal(1, result[0].Id);
        }

        [Fact]
        public void WhereSimple2()
        {
            var result = TestClass.CreateQuery().OData().Where("Name eq 'n1'").ToArray();

            Assert.Single(result);
            Assert.Equal("n1", result[0].Name);            
        }
    }
}