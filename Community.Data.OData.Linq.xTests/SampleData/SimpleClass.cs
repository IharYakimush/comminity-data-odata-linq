using System.Runtime.Serialization;
using Community.OData.Linq.Annotations;

namespace Community.OData.Linq.xTests.SampleData
{
    using System;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Linq;

    public class SimpleClass
    {
        private static readonly SimpleClass[] items =
        {
            new SimpleClass {Id = 1, Name = "n1", DateTime = new DateTime(2018, 1, 26), TestEnum = TestEnum.Item1, NameToIgnore = "ni1", NameNotFilter="nf1"},
            new SimpleClass {Id = 2, Name = "n2", DateTime = new DateTime(2001, 1, 26), TestEnum = TestEnum.Item2, NameToIgnore = "ni1", NameNotFilter="nf2"}
        };
        
        public static IQueryable<SimpleClass> CreateQuery()
        {
            return items.AsQueryable();
        }

        public int Id { get; set; }

        public string Name { get; set; }

        public DateTime DateTime { get; set; }

        public TestEnum TestEnum { get; set; }

        [IgnoreDataMember]
        public string NameToIgnore { get; set; }

        [NonFilterable]
        public string NameNotFilter { get; set; }

        [NotSortable]
        public int NotOrderable { get; set; }

        [NotMapped]
        public int NotMapped { get; set; }
    }    
}
