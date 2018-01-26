namespace Community.OData.Linq
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Design;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Linq.Expressions;

    using Community.OData.Linq.Builder;
    using Community.OData.Linq.OData;
    using Community.OData.Linq.OData.Query;
    using Community.OData.Linq.OData.Query.Expressions;

    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.OData;
    using Microsoft.OData.Edm;
    using Microsoft.OData.UriParser;

    public static class ODataLinqExtensions
    {
        /// <summary>
        /// The simplified options.
        /// </summary>
        private static readonly ODataSimplifiedOptions SimplifiedOptions = new ODataSimplifiedOptions();

        public static ODataQuery<T> Filter<T>(this ODataQuery<T> query, string filterText, string entitySetName = null)
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
            
            ODataQueryOptionParser queryOptionParser = new ODataQueryOptionParser(
                edmModel,
                path,
                new Dictionary<string, string> { { "$filter", filterText } },
                query.ServiceProvider);

            // Workaround for strange behavior in QueryOptionsParserConfiguration constructor which set it to false always
            queryOptionParser.Resolver.EnableCaseInsensitive =
                query.ServiceProvider.GetRequiredService<ODataSettings>().EnableCaseInsensitive;          

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

        /// <summary>
        /// Enable applying OData specific functions to query
        /// </summary>
        /// <param name="query">
        /// The query.
        /// </param>
        /// <param name="configuration">
        /// The configuration action.
        /// </param>
        /// <param name="edmModel">
        /// The edm model.
        /// </param>
        /// <typeparam name="T">        
        /// The query type param
        /// </typeparam>
        /// <returns>
        /// The <see cref="ODataQuery{T}"/> query.
        /// </returns>
        public static ODataQuery<T> OData<T>(this IQueryable<T> query, Action<ODataSettings> configuration = null, IEdmModel edmModel = null)
        {
            if (edmModel == null)
            {
                ODataConventionModelBuilder builder = new ODataConventionModelBuilder();
                builder.AddEntityType(typeof(T));
                builder.AddEntitySet(typeof(T).Name, new EntityTypeConfiguration(new ODataModelBuilder(), typeof(T)));
                edmModel = builder.GetEdmModel();
            }

            ODataSettings settings = new ODataSettings();
            configuration?.Invoke(settings);
            
            ServiceContainer container = new ServiceContainer();
            container.AddService(typeof(IEdmModel), edmModel);
            container.AddService(typeof(ODataQuerySettings), settings.QuerySettings);
            container.AddService(typeof(ODataUriParserSettings), settings.ParserSettings);
            container.AddService(typeof(IAssembliesResolver), new DefaultAssembliesResolver());
            container.AddService(typeof(FilterBinder), new FilterBinder(container));
            container.AddService(typeof(ODataUriResolver), settings.Resolver ?? ODataSettings.DefaultResolver);
            container.AddService(typeof(ODataSimplifiedOptions), SimplifiedOptions);
            container.AddService(typeof(ODataSettings), settings);

            ODataQuery<T> dataQuery = new ODataQuery<T>(query, container);

            return dataQuery;
        }
    }
}
