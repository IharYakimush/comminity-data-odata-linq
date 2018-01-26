namespace Community.OData.Linq.xTests
{
    using System;
    using System.ComponentModel.Design;

    using Community.OData.Linq.xTests.SampleData;

    using Xunit;

    public class ODataQueryTests
    {
        [Fact]
        public void Constructor()
        {
            ODataQuery<SimpleClass> query = new ODataQuery<SimpleClass>(SimpleClass.CreateQuery(), new ServiceContainer());

            Assert.Throws<ArgumentNullException>(() => new ODataQuery<SimpleClass>(null, null));
        }
    }
}
