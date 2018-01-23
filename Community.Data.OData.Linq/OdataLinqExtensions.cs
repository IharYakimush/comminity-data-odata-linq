using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics.Contracts;
using System.Linq.Expressions;
using System.Web.OData;
using System.Web.OData.Query;
using System.Web.OData.Query.Expressions;

namespace Community.Data.OData.Linq
{
    using System;
    using System.Linq;
    using Community.Data.OData.Linq.EdmModel;
    using Microsoft.OData.Edm;
    using Microsoft.OData.UriParser;

    public static class ODataLinqExtensions
    {
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
                path, new Dictionary<string, string> {{"$filter", filterText}});
            
            FilterClause filterClause = queryOptionParser.ParseFilter();
            SingleValueNode filterExpression = filterClause.Expression.Accept(
                new ParameterAliasNodeTranslator(queryOptionParser.ParameterAliasNodes)) as SingleValueNode;
            filterExpression = filterExpression ?? new ConstantNode(null);
            filterClause = new FilterClause(filterExpression, filterClause.RangeVariable);
            Contract.Assert(filterClause != null);            

            Expression filter = FilterBinder.Bind(query, filterClause, typeof(T), query.ServiceProvider);
            var result = ExpressionHelpers.Where(query, filter, typeof(T));

            return new ODataQuery<T>(result, query.ServiceProvider);
        }

        public static ODataQuery<T> OData<T>(this IQueryable<T> query, ODataQuerySettings querySettings = null, IEdmModel edmModel = null)
        {
            if (edmModel == null)
            {
                edmModel = Helper.Build(typeof(T));
            }

            if (querySettings == null)
            {
                querySettings = new ODataQuerySettings();
            }

            ServiceContainer container = new ServiceContainer();
            container.AddService(typeof(IEdmModel), edmModel);
            container.AddService(typeof(ODataQuerySettings), querySettings);
            container.AddService(typeof(IAssembliesResolver), new DefaultAssembliesResolver());
            container.AddService(typeof(FilterBinder), new FilterBinder(container));

            ODataQuery<T> dataQuery = new ODataQuery<T>(query, container);

            return dataQuery;
        }
    }
}
