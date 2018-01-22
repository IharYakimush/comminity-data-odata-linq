namespace Community.Data.OData.Linq.Tests
{
    using System;

    using Xunit;
    using Microsoft.OData.Edm;
    public class ODataQueryTests
    {
        [Fact]
        public void Constructor()
        {
            ODataQuery<TestClass> query = new ODataQuery<TestClass>(TestClass.CreateQuery(), new EdmModel());

            Assert.Throws<ArgumentNullException>(() => new ODataQuery<TestClass>(null, null));
        }
    }
}
