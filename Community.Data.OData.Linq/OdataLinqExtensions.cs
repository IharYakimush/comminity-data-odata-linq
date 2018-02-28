namespace Community.OData.Linq
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Design;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Linq.Expressions;
    
    using Community.OData.Linq.Builder;
    using Community.OData.Linq.Builder.Validators;
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
            if (query == null) throw new ArgumentNullException(nameof(query));

            if (edmModel == null)
            {                
                ODataConventionModelBuilder builder = new ODataConventionModelBuilder();
                builder.AddEntityType(typeof(T));
                builder.AddEntitySet(typeof(T).Name, new EntityTypeConfiguration(new ODataModelBuilder(), typeof(T)));
                edmModel = builder.GetEdmModel();
            }
            else
            {
                if (edmModel.SchemaElements.Count(e => e.SchemaElementKind == EdmSchemaElementKind.EntityContainer) == 0)
                {
                    throw new ArgumentException("Provided Entity Model have no IEdmEntityContainer", nameof(edmModel));
                }
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
            container.AddService(typeof(DefaultQuerySettings), settings.DefaultQuerySettings);
            container.AddService(typeof(SelectExpandQueryValidator), new SelectExpandQueryValidator(settings.DefaultQuerySettings));

            ODataQuery<T> dataQuery = new ODataQuery<T>(query, container);

            return dataQuery;
        }

        /// <summary>
        /// The select and expand query options.
        /// </summary>
        /// <param name="query">
        /// The OData aware query.
        /// </param>
        /// <param name="selectText">
        /// The $select parameter text.
        /// </param>
        /// <param name="expandText">
        /// The $expand parameter text.
        /// </param>
        /// <param name="entitySetName">
        /// The entity set name.
        /// </param>
        /// <typeparam name="T">
        /// The query type param
        /// </typeparam>
        /// <returns>
        /// The <see cref="IEnumerable{ISelectExpandWrapper}"/> selection result in specific format.
        /// </returns>
        public static IEnumerable<ISelectExpandWrapper> SelectExpand<T>(
            this ODataQuery<T> query,
            string selectText = null,
            string expandText = null,
            string entitySetName = null)
        {            
            SelectExpandHelper<T> helper = new SelectExpandHelper<T>(
                new ODataRawQueryOptions { Select = selectText, Expand = expandText },
                query,
                entitySetName);

            helper.AddAutoSelectExpandProperties();
            
            var result = helper.Apply(query);

            // In case of SelectExpand ,ethod was called to convert to ISelectExpandWrapper without actually applying $select and $expand params
            if (result == query && selectText==null && expandText == null)
            {
                return SelectExpand(query, "*", expandText, entitySetName);
            }

            return result.OfType<ISelectExpandWrapper>();
        }

        /// <summary>
        /// The Filter.
        /// </summary>
        /// <param name="query">
        /// The OData aware query.
        /// </param>
        /// <param name="filterText">
        /// The $filter parameter text.
        /// </param>
        /// <param name="entitySetName">
        /// The entity set name.
        /// </param>
        /// <typeparam name="T">
        /// The query type param
        /// </typeparam>
        /// <returns>
        /// The <see cref="ODataQuery{T}"/> query with applied filter parameter.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Argument Null Exception
        /// </exception>
        public static ODataQuery<T> Filter<T>(this ODataQuery<T> query, string filterText, string entitySetName = null)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (filterText == null) throw new ArgumentNullException(nameof(filterText));

            IEdmModel edmModel = query.EdmModel;

            ODataQueryOptionParser queryOptionParser = GetParser(
                query,
                entitySetName,
                new Dictionary<string, string> { { "$filter", filterText } });

            ODataSettings settings = query.ServiceProvider.GetRequiredService<ODataSettings>();

            FilterClause filterClause = queryOptionParser.ParseFilter();
            SingleValueNode filterExpression = filterClause.Expression.Accept(
                new ParameterAliasNodeTranslator(queryOptionParser.ParameterAliasNodes)) as SingleValueNode;
            filterExpression = filterExpression ?? new ConstantNode(null);
            filterClause = new FilterClause(filterExpression, filterClause.RangeVariable);
            Contract.Assert(filterClause != null);

            var validator = new FilterQueryValidator(settings.DefaultQuerySettings);
            validator.Validate(filterClause, settings.ValidationSettings, edmModel);

            Expression filter = FilterBinder.Bind(query, filterClause, typeof(T), query.ServiceProvider);
            var result = ExpressionHelpers.Where(query, filter, typeof(T));

            return new ODataQuery<T>(result, query.ServiceProvider);
        }

        /// <summary>
        /// The OrderBy.
        /// </summary>
        /// <param name="query">
        /// The OData aware query.
        /// </param>
        /// <param name="orderbyText">
        /// The $orderby parameter text.
        /// </param>
        /// <param name="entitySetName">
        /// The entity set name.
        /// </param>
        /// <typeparam name="T">
        /// The query type param
        /// </typeparam>
        /// <returns>
        /// The <see cref="ODataQuery{T}"/> query with applied order by parameter.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Argument Null Exception
        /// </exception>
        public static IOrderedQueryable<T> OrderBy<T>(this ODataQuery<T> query, string orderbyText, string entitySetName = null)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (orderbyText == null) throw new ArgumentNullException(nameof(orderbyText));

            IEdmModel edmModel = query.EdmModel;

            ODataQueryOptionParser queryOptionParser = GetParser(
                query,
                entitySetName,
                new Dictionary<string, string> { { "$orderby", orderbyText } });

            ODataSettings settings = query.ServiceProvider.GetRequiredService<ODataSettings>();

            var orderByClause = queryOptionParser.ParseOrderBy();

            orderByClause = TranslateParameterAlias(orderByClause, queryOptionParser);

            ICollection<OrderByNode> nodes = OrderByNode.CreateCollection(orderByClause);

            var validator = new OrderByQueryValidator(settings.DefaultQuerySettings);
            validator.Validate(nodes, settings.ValidationSettings, edmModel);

            IOrderedQueryable<T> result = (IOrderedQueryable<T>)OrderByBinder.OrderApplyToCore(query, settings.QuerySettings, nodes, edmModel);

            return new ODataQueryOrdered<T>(result, query.ServiceProvider);
        }        

        private static OrderByClause TranslateParameterAlias(OrderByClause orderBy, ODataQueryOptionParser queryOptionParser)
        {
            if (orderBy == null)
            {
                return null;
            }

            SingleValueNode orderByExpression = orderBy.Expression.Accept(
                new ParameterAliasNodeTranslator(queryOptionParser.ParameterAliasNodes)) as SingleValueNode;
            orderByExpression = orderByExpression ?? new ConstantNode(null, "null");

            return new OrderByClause(
                TranslateParameterAlias(orderBy.ThenBy, queryOptionParser),
                orderByExpression,
                orderBy.Direction,
                orderBy.RangeVariable);
        }

        public static ODataQueryOptionParser GetParser<T>(ODataQuery<T> query, string entitySetName, IDictionary<string, string> raws)
        {
            IEdmModel edmModel = query.EdmModel;

            if (entitySetName == null)
            {
                entitySetName = typeof(T).Name;
            }

            IEdmEntityContainer[] containers =
                edmModel.SchemaElements.Where(
                        e => e.SchemaElementKind == EdmSchemaElementKind.EntityContainer &&
                             (e as IEdmEntityContainer).FindEntitySet(entitySetName) != null)
                    .OfType<IEdmEntityContainer>()
                    .ToArray();

            if (containers.Length == 0)
            {
                throw new ArgumentException($"Unable to find {entitySetName} entity set in the model.",
                    nameof(entitySetName));
            }

            if (containers.Length > 1)
            {
                throw new ArgumentException($"Entity Set {entitySetName} found more that 1 time",
                    nameof(entitySetName));
            }

            IEdmEntitySet entitySet = containers.Single().FindEntitySet(entitySetName);

            if (entitySet == null)
            {
                
            }

            ODataPath path = new ODataPath(new EntitySetSegment(entitySet));

            ODataQueryOptionParser parser = new ODataQueryOptionParser(edmModel, path, raws, query.ServiceProvider);

            ODataSettings settings = query.ServiceProvider.GetRequiredService<ODataSettings>();

            // Workaround for strange behavior in QueryOptionsParserConfiguration constructor which set it to false always
            parser.Resolver.EnableCaseInsensitive = settings.EnableCaseInsensitive;

            return parser;
        }
    }
}
