using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using Xunit;
using Community.OData.Linq.Json;

namespace Community.OData.Linq.xTests.Issues29
{
    class MyClassA
    {
        [Key]
        public string String { get; set; }

        public List<MyClassB> Subs { get; set; }
    }

    class MyClassB
    {
        [Key]
        public int Integer { get; set; }
    }

    public class Issue29
    {
        [Fact]
        public void SubLevelFilterTest()
        {
            ODataQuery<MyClassA> odataQuery = GetSampleData().OData();
            odataQuery = odataQuery.Filter("String eq 'A'");

            // This is what I used until now; good result
            string serializeCorrect = JsonConvert.SerializeObject(odataQuery);

            IEnumerable<ISelectExpandWrapper> collection = GetSampleData().OData().Filter("String eq 'A'").SelectExpand("String", "Subs($filter=Integer eq 1)");            
            string result1 = collection.ToJson().ToString();

            MyClassA[] array = GetSampleData().OData().Filter("String eq 'A'").ToArray();
            foreach (MyClassA item in array)
            {
                item.Subs = item.Subs.AsQueryable().OData().Filter("Integer eq 1").ToList();
            }

            string result2 = JsonConvert.SerializeObject(array);
        }

        private static IQueryable<MyClassA> GetSampleData()
        {
            return new List<MyClassA>()
            {
                new MyClassA()
                {
                    String = "A",
                    Subs = new List<MyClassB>() {{new MyClassB() {Integer = 1}}, {new MyClassB() {Integer = 2}}, {new MyClassB() {Integer = 3}}, {new MyClassB() {Integer = 4}}}
                },
                new MyClassA()
                {
                    String = "B",
                    Subs = new List<MyClassB>() {{new MyClassB() {Integer = 1}}, {new MyClassB() {Integer = 2}}, {new MyClassB() {Integer = 3}}, {new MyClassB() {Integer = 4}}}
                }
            }.AsQueryable();
        }
    }
}
