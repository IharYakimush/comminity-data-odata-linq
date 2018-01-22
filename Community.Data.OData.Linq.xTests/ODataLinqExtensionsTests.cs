namespace Community.Data.OData.Linq.Tests
{
    using System.Linq;
    using Xunit;


    public class ODataLinqExtensionsTests
    {
        [Fact]
        public void WhereFirstTime()
        {
            var result = TestClass.CreateQuery().OData().Where("Id eq 1").ToArray();
            
            Assert.Single(result);
            Assert.Equal(1, result[0].Id);
        }
    }
}