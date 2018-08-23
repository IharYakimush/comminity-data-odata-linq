using System.Collections.Generic;
using System.Runtime.Serialization;
using SolrNet.Attributes;

namespace Community.OData.Linq.SolrNetLinqTests
{
    public class Product

    {

        [DataMember]
        [SolrUniqueKey("id")]
        public string Id { get; set; }

        [DataMember]
        [SolrField("manu_exact")]
        public string Manufacturer { get; set; }
    
        [SolrField("cat")]
        public ICollection<string> Categories { get; set; }

        [DataMember]
        [SolrField("price")]
        public decimal Price { get; set; }

        [SolrField("sequence_i")]
        public int Sequence { get; set; }

        [SolrField("popularity")]
        public decimal? Popularity { get; set; }

        public decimal NotMapped { get; set; }

        [SolrField("inStock_b")]
        public bool InStock { get; set; }        
    }
}