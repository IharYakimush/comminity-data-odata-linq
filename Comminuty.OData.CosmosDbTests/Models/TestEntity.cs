using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Comminuty.OData.CosmosDbTests.Models
{
    public class TestEntity
    {
        public static TestEntity Create()
        {
            return new TestEntity() { 
                Id = Guid.NewGuid().ToString(),
                Number = 1,
                Pk = "qwe",
                Item = TestEntity3.Create(),
                Childs = new[] { TestEntity2.Create(), TestEntity2.Create(), TestEntity2.Create(), TestEntity2.Create(), TestEntity2.Create() }
            };
        }

        public string Id { get; set; }
        public string Pk { get; set; }

        public int Number { get; set; }

        public ICollection<TestEntity2> Childs { get; set;}

        public TestEntity3 Item { get; set; }
    }

    public class TestEntity2
    {
        public static TestEntity2 Create()
        {
            return new TestEntity2()
            {
                Id = Guid.NewGuid().ToString(),
                Number = 2,
                Pk = "qwe",
                Item = TestEntity3.Create()
            };
        }

        public string Id { get; set; }
        public string Pk { get; set; }

        public int Number { get; set; }

        public TestEntity3 Item { get; set; }
    }

    public class TestEntity3
    {
        public static TestEntity3 Create()
        {
            return new TestEntity3()
            {
                Id = Guid.NewGuid().ToString(),
                Number = 3,
                Pk = "qwe"                
            };
        }

        public string Id { get; set; }
        public string Pk { get; set; }

        public int Number { get; set; }
    }
}
