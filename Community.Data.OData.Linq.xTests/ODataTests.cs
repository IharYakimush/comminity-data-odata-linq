namespace Community.OData.Linq.xTests
{
    using System;
    using System.Collections.Generic;
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

        [Fact]
        public void Disposable()
        {
            // Generate the list of items to query
            var items = new List<TestItem>();
            items.Add(new TestItem() { Id = Guid.NewGuid(), Name = "Test", Number = 1 });
            items.Add(new TestItem() { Id = Guid.NewGuid(), Name = "Another", Number = 2 });

            var odata = items.AsQueryable().OData();
            var filteredItems = odata.Filter("Number eq 2");
            //odata.Dispose();                                        // Test that the OData object is being torn down

            Assert.Equal(1, filteredItems.Count());
        }
    }
}