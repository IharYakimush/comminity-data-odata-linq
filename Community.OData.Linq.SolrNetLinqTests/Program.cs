using System;
using System.Linq;
using CommonServiceLocator;
using Community.OData.Linq.Json;
using SolrNet;
using SolrNet.Linq;

namespace Community.OData.Linq.SolrNetLinqTests
{
    class Program
    {
        static void Main(string[] args)
        {
            Startup.Init<Product>("http://localhost:8983/solr/demo");

            ISolrOperations<Product> solr = ServiceLocator.Current.GetInstance<ISolrOperations<Product>>();

            //IQueryable<ISelectExpandWrapper> sr = solr.AsQueryable().OData().Filter("Id ne null and Price gt 0").OrderBy("Id desc").TopSkip("1", "1")
            //    .SelectExpandAsQueryable("Id,Price,Categories");

            //Console.WriteLine(sr.GetType());
            //Console.WriteLine(sr.ToJson());

            SolrQueryResults<ISelectExpandWrapper> results = solr.AsQueryable().OData().Filter("Id ne null").OrderBy("Id desc").TopSkip("1", "1")
                .SelectExpandAsQueryable("Id,Price,Categories").ToSolrQueryResults();

            Console.WriteLine(results.NumFound);
            Console.WriteLine(results.ToJson());
        }
    }
}
