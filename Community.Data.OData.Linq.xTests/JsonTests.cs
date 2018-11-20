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
            JToken token = ClassWithCollection.CreateQuery().OData().SelectExpand("Name", "Link2($filter=Id eq 311;$select=Name)").ToJson();
            Assert.NotNull(token);

            Assert.DoesNotContain("ModelID", token.ToString(Formatting.None));
        }

        [Fact]
        public void SerializeSelectExpand2()
        {
            JToken token = ClassWithCollection.CreateQuery().OData().SelectExpand("Name", "Link2($filter=Id eq 311;$select=Name)").ToJson();
            Assert.NotNull(token);

            Assert.DoesNotContain("ModelID", token.ToString(Formatting.None));
        }
    }
}