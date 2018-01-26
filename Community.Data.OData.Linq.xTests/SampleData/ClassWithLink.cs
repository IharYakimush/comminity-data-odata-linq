namespace Community.OData.Linq.xTests.SampleData
{
    using System.Linq;

    public class ClassWithLink
    {
        private static readonly ClassWithLink[] items = new[]
        {
            new ClassWithLink {Id = 21, Name = "n21", Link1 = new SimpleClass {Id = 211, Name = "n211"}},
            new ClassWithLink {Id = 22, Name = "n22", Link1 = new SimpleClass {Id = 221, Name = "n221"}}
        };

        public static IQueryable<ClassWithLink> CreateQuery()
        {
            return items.AsQueryable();
        }

        public long Id { get; set; }

        public string Name { get; set; }

        public virtual SimpleClass Link1 { get; set; }
    }
}