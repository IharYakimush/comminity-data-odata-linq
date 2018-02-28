namespace Community.OData.Linq.xTests
{
    using System.Linq;

    using Community.OData.Linq.xTests.SampleData;
    using Community.OData.Linq.Json;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    using Xunit;

    public class JsonTests
    {
        [Fact]
        public void SerializeSelectExpand()
        {
            string result = ClassWithCollection.CreateQuery().OData().SelectExpandJsonString("Name", "Link2($filter=Id eq 311;$select=Name)");

            Assert.NotNull(result);

            JToken token = ClassWithCollection.CreateQuery().OData().SelectExpandJsonToken("Name", "Link2($filter=Id eq 311;$select=Name)");
            Assert.NotNull(token);

            Assert.Equal(result, token.ToString(Formatting.None));
        }
    }
}