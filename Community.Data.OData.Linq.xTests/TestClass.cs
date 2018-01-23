namespace Community.Data.OData.Linq.Tests
{
    using System.Linq;
    public class TestClass
    {
        private static readonly TestClass[] items = { new TestClass { Id = 1, Name = "n1" }, new TestClass { Id = 2, Name = "n2" } };
        
        public static IQueryable<TestClass> CreateQuery()
        {
            return items.AsQueryable();
        }

        public int Id { get; set; }

        public string Name { get; set; }
    }    
}
