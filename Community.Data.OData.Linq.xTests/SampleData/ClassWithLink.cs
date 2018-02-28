namespace Community.OData.Linq.xTests.SampleData
{
    using System.Linq;

    using Community.OData.Linq.Annotations;

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

        [NotNavigable]
        public virtual SimpleClass Link2 { get; set; }

        [Select(SelectType = OData.Query.SelectExpandType.Automatic)]
        public virtual SimpleClass Link3 { get; set; }

        [Select(SelectType = OData.Query.SelectExpandType.Disabled)]
        public virtual SimpleClass Link4 { get; set; }
    }
}