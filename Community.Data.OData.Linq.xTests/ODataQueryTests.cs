namespace Community.Data.OData.Linq.Tests
{
    using System;
    using Microsoft.Data.Edm.Library;

    using Xunit;

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
