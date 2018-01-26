namespace Community.OData.Linq.xTests.SampleData
{
    using System;
    using System.Linq;

    public class SimpleClass
    {
        private static readonly SimpleClass[] items = { new SimpleClass { Id = 1, Name = "n1", DateTime = new DateTime(2018, 1, 26), TestEnum = TestEnum.Item1 }, new SimpleClass { Id = 2, Name = "n2", DateTime = new DateTime(2001, 1, 26), TestEnum = TestEnum.Item2 } };
        
        public static IQueryable<SimpleClass> CreateQuery()
        {
            return items.AsQueryable();
        }

        public int Id { get; set; }

        public string Name { get; set; }

        public DateTime DateTime { get; set; }

        public TestEnum TestEnum { get; set; }
    }    
}
