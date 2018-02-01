namespace Community.OData.Linq.xTests.SampleData
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;

    public class SampleWithCustomKey
    {
        private static readonly SampleWithCustomKey[] items =
            {
                new SampleWithCustomKey { Name = "n1", DateTime = new DateTime(2018, 1, 26)},
                new SampleWithCustomKey { Name = "n2", DateTime = new DateTime(2001, 1, 26)}
            };

        public static IQueryable<SampleWithCustomKey> CreateQuery()
        {
            return items.AsQueryable();
        }

        [Key]
        public string Name { get; set; }

        public DateTime DateTime { get; set; }
    }
}