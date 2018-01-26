namespace Community.OData.Linq.xTests.SampleData
{
    using System.Linq;
    using System.Runtime.Serialization;

    [DataContract]
    public class SimpleClassDataContract
    {
        private static readonly SimpleClass[] items = { new SimpleClass { Id = 1, Name = "n1" }, new SimpleClass { Id = 2, Name = "n2" } };
        
        public static IQueryable<SimpleClass> CreateQuery()
        {
            return items.AsQueryable();
        }

        [DataMember]
        public int Id { get; set; }

        [DataMember(Name = "nameChanged")]
        public string Name { get; set; }

        [IgnoreDataMember]
        public string NameToIgnore { get; set; }
    }    
}
