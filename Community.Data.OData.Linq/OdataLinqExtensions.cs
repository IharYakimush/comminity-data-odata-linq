using System.Collections.Generic;
using System.Web.OData;
using System.Web.OData.Query;

namespace Community.Data.OData.Linq
{
    using System;
    using System.Linq;
    using Community.Data.OData.Linq.EdmModel;
    using Microsoft.OData.Edm;
    using Microsoft.OData.UriParser;

    public static class ODataLinqExtensions
    {
        private static readonly Uri Root = new Uri("http://foo.com");
        public static ODataQuery<T> Where<T>(this ODataQuery<T> query, string filterText, string entitySetName = null)
        {
            IEdmModel edmModel = query.EdmModel;
            
            if (entitySetName == null)
            {
                entitySetName = typeof(T).Name;
            }

            IEdmEntityContainer container =
                (IEdmEntityContainer)edmModel.SchemaElements.Single(
                    e => e.SchemaElementKind == EdmSchemaElementKind.EntityContainer);

            IEdmEntitySet entitySet = container.FindEntitySet(entitySetName);
            ODataPath path = new ODataPath(new EntitySetSegment(entitySet));

            ODataQueryOptionParser queryOptionParser = new ODataQueryOptionParser(edmModel,
                path, new Dictionary<string, string>());

           

            return query;
        }

        public static ODataQuery<T> OData<T>(this IQueryable<T> query, IEdmModel edmModel = null)
        {
            if (edmModel == null)
            {
                edmModel = Helper.Build(typeof(T));
            }
            
            ODataQuery<T> dataQuery = new ODataQuery<T>(query, edmModel);

            return dataQuery;
        }
    }
}
