
namespace Community.OData.Linq.xTests.Issues
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Linq;

    using Community.OData.Linq.xTests.SampleData;

    using Microsoft.OData;

    using Xunit;

    public class Issue31
    {
        [Fact]
        public void WhereWithInThrowException()
        {
            Exception exc = Assert.Throws<ODataException>(() => SimpleClass.CreateQuery().OData().Filter("Id in (1,100)").ToArray());
            Assert.Contains("Syntax error", exc.Message);
        }
    }
}
