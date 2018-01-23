using System.Linq;

namespace Community.Data.OData.Linq.Tests
{
    public class TestClass2
    {
        private static readonly TestClass2[] items = new[]
        {
            new TestClass2 {Id = 21, Name = "n21", Link1 = new TestClass {Id = 211, Name = "n211"}},
            new TestClass2 {Id = 22, Name = "n22", Link1 = new TestClass {Id = 221, Name = "n221"}}
        };

        public static IQueryable<TestClass2> CreateQuery()
        {
            return items.AsQueryable();
        }

        public long Id { get; set; }

        public string Name { get; set; }

        public virtual TestClass Link1 { get; set; }
    }
}