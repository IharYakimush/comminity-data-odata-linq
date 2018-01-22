using System;
using System.Linq;

namespace Community.Data.OData.Linq
{
    using Community.Data.OData.Linq.EdmModel;

    using Microsoft.Data.Edm;
    using Microsoft.Data.OData.Query;
    using Microsoft.Data.OData.Query.SemanticAst;

    public static class ODataLinqExtensions
    {
        private static readonly Uri Root = new Uri("http://foo.com");
        public static ODataQuery<T> Where<T>(this ODataQuery<T> query, string filterText, string entitySetName = null)
        {
            IEdmModel edmModel = query.EdmModel;
            
            ODataUriParser parser = new ODataUriParser(edmModel, Root);
            IEdmEntityContainer container =
                (IEdmEntityContainer)edmModel.SchemaElements.Single(
                    e => e.SchemaElementKind == EdmSchemaElementKind.EntityContainer);

            if (entitySetName == null)
            {
                entitySetName = typeof(T).Name;
            }

            IEdmEntitySet entitySet = container.FindEntitySet(entitySetName);

            FilterClause filterClause = parser.ParseFilter(filterText, entitySet.ElementType, entitySet);

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
