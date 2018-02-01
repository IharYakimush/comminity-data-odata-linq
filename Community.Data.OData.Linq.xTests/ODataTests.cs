namespace Community.OData.Linq.xTests
{
    using System;
    using System.Linq;

    using Community.OData.Linq.xTests.SampleData;

    using Microsoft.OData;

    using Xunit;

    public class ODataTests
    {
        [Fact]
        public void CustomKey()
        {
            var result = SampleWithCustomKey.CreateQuery().OData().Filter("Name eq 'n1'").ToArray();

            Assert.Single(result);
            Assert.Equal("n1", result[0].Name);
        }

        [Fact]
        public void WithoutKeyThrowException()
        {
            Assert.Throws<InvalidOperationException>(() => SampleWithoutKey.CreateQuery().OData());
        }
    }
}