namespace Community.OData.Linq
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.ComponentModel.Design;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Linq.Expressions;
    
    using Community.OData.Linq.Builder;
    using Community.OData.Linq.Builder.Validators;
    using Community.OData.Linq.Common;
    using Community.OData.Linq.OData;
    using Community.OData.Linq.OData.Query;
    using Community.OData.Linq.OData.Query.Expressions;
    using Community.OData.Linq.Properties;

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

        private static readonly ConcurrentDictionary<Type,IEdmModel> Models = new ConcurrentDictionary<Type, IEdmModel>();

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
        /// The OData aware <see cref="ODataQuery{T}"/> query.
        /// </returns>
        public static ODataQuery<T> OData<T>(this IQueryable<T> query, Action<ODataSettings> configuration = null, IEdmModel edmModel = null)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));

            if (edmModel == null)
            {
                edmModel = Models.GetOrAdd(typeof(T), t =>
                {
                    ODataConventionModelBuilder builder = new ODataConventionModelBuilder();
                    builder.AddEntityType(t);
                    builder.AddEntitySet(t.Name, new EntityTypeConfiguration(new ODataModelBuilder(), t));
                    return builder.GetEdmModel();
                });
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
        /// Apply select and expand query options.
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
            var result = SelectExpandInternal(query, selectText, expandText, entitySetName);

            return Enumerate<ISelectExpandWrapper>(result);
        }

        /// <summary>
        /// Apply select and expand query options to query. Warning! not all query providers support it.
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
        /// The <see cref="IQueryable{ISelectExpandWrapper}"/> selection result in specific format.
        /// </returns>
        public static IQueryable<ISelectExpandWrapper> SelectExpandAsQueryable<T>(
            this ODataQuery<T> query,
            string selectText = null,
            string expandText = null,
            string entitySetName = null)
        {
            IQueryable result = SelectExpandInternal(query, selectText, expandText, entitySetName);
            return query.Provider.CreateQuery<ISelectExpandWrapper>(result.Expression).Cast<ISelectExpandWrapper>();
            //return result as IQueryable<ISelectExpandWrapper>;
        }

        private static IQueryable SelectExpandInternal<T>(ODataQuery<T> query, string selectText, string expandText,
            string entitySetName)
        {
            SelectExpandHelper<T> helper = new SelectExpandHelper<T>(
                new ODataRawQueryOptions { Select = selectText, Expand = expandText },
                query,
                entitySetName);

            helper.AddAutoSelectExpandProperties();

            var result = helper.Apply(query);

            // In case of SelectExpand ,method was called to convert to ISelectExpandWrapper without actually applying $select and $expand params
            if (result == query && selectText == null && expandText == null)
            {
                return SelectExpandInternal(query, "*", expandText, entitySetName);
            }

            return result;
        }

        /// <summary>
        /// Apply $top and $skip parameters to query.
        /// </summary>
        /// <typeparam name="T">The type param</typeparam>
        /// <param name="query">The OData aware query.</param>
        /// <param name="topText">$top parameter value</param>
        /// <param name="skipText">$skip parameter value</param>
        /// <param name="entitySetName">The entity set name.</param>
        /// <returns>The <see cref="ODataQuery{T}"/> query with applied $top and $skip parameters.</returns>
        public static ODataQuery<T> TopSkip<T>(this ODataQuery<T> query, string topText = null, string skipText = null, string entitySetName = null)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));            

            ODataSettings settings = query.ServiceProvider.GetRequiredService<ODataSettings>();

            Dictionary<string, string> dictionary = new Dictionary<string, string>();

            if (topText != null)
            {
                dictionary.Add("$top", topText);                
            }

            if (skipText != null)
            {
                dictionary.Add("$skip", skipText);
            }

            ODataQueryOptionParser queryOptionParser = GetParser(
                query,
                entitySetName,
                dictionary);

            long? skip = queryOptionParser.ParseSkip();
            long? top = queryOptionParser.ParseTop();

            if (skip.HasValue || top.HasValue || settings.QuerySettings.PageSize.HasValue)
            {
                IQueryable<T> result = TopSkipHelper.ApplySkipWithValidation(query, skip, settings);
                if (top.HasValue)
                {
                    result = TopSkipHelper.ApplyTopWithValidation(result, top, settings);
                }
                else
                {
                    result = TopSkipHelper.ApplyTopWithValidation(result, settings.QuerySettings.PageSize, settings);
                }

                return new ODataQuery<T>(result, query.ServiceProvider);
            }

            return query;
        }

        /// <summary>
        /// Apply OData query options except $select and $expand parameters.
        /// </summary>
        /// <typeparam name="T">The type param.</typeparam>
        /// <param name="query">The OData aware query.</param>
        /// <param name="rawQueryOptions">The query options.</param>
        /// <param name="entitySetName">The entity set name.</param>
        /// <returns>The query <see cref="IQueryable{T}"/> with applied OData parameters.</returns>
        public static IQueryable<T> ApplyQueryOptionsWithoutSelectExpand<T>(
            this ODataQuery<T> query,
            IODataQueryOptions rawQueryOptions,
            string entitySetName = null)
        {
            return ApplyQueryOptionsInternal(query, rawQueryOptions, entitySetName);
        }

        /// <summary>
        /// Apply OData query options and execute query.
        /// </summary>
        /// <typeparam name="T">The type param.</typeparam>
        /// <param name="query">The OData aware query.</param>
        /// <param name="rawQueryOptions">The query options.</param>
        /// <param name="entitySetName">The entity set name.</param>
        /// <returns>The enumeration of query results <see cref="IEnumerable{ISelectExpandWrapper}"/>.</returns>
        public static IEnumerable<ISelectExpandWrapper> ApplyQueryOptions<T>(
            this ODataQuery<T> query,
            IODataQueryOptions rawQueryOptions,
            string entitySetName = null)
        {
            return ApplyQueryOptionsInternal(query, rawQueryOptions, entitySetName).SelectExpand(
                rawQueryOptions.Select,
                rawQueryOptions.Expand,
                entitySetName);
        }

        /// <summary>
        /// Apply OData query options. Warning! not all providers support it.
        /// </summary>
        /// <typeparam name="T">The type param.</typeparam>
        /// <param name="query">The OData aware query.</param>
        /// <param name="rawQueryOptions">The query options.</param>
        /// <param name="entitySetName">The entity set name.</param>
        /// <returns>The query with special type of results <see cref="IQueryable{ISelectExpandWrapper}"/>.</returns>
        public static IQueryable<ISelectExpandWrapper> ApplyQueryOptionsAsQueryable<T>(
            this ODataQuery<T> query,
            IODataQueryOptions rawQueryOptions,
            string entitySetName = null)
        {
            return ApplyQueryOptionsInternal(query, rawQueryOptions, entitySetName).SelectExpandAsQueryable(
                rawQueryOptions.Select,
                rawQueryOptions.Expand,
                entitySetName);
        }

        private static ODataQuery<T> ApplyQueryOptionsInternal<T>(ODataQuery<T> query, IODataQueryOptions rawQueryOptions, string entitySetName)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (rawQueryOptions == null) throw new ArgumentNullException(nameof(rawQueryOptions));

            if (rawQueryOptions.Filters != null)
            {
                foreach (string filter in rawQueryOptions.Filters)
                {
                    query = query.Filter(filter, entitySetName);
                }                
            }

            if (rawQueryOptions.OrderBy != null)
            {
                query = query.OrderBy(rawQueryOptions.OrderBy, entitySetName);
            }

            query = query.TopSkip(rawQueryOptions.Top, rawQueryOptions.Skip);

            return query;
        }

        /// <summary>
        /// Apply $filter parameter to query.
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
        /// Apply $orderby parameter to query.
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
        public static ODataQueryOrdered<T> OrderBy<T>(this ODataQuery<T> query, string orderbyText, string entitySetName = null)
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

        private static IEnumerable<T> Enumerate<T>(IQueryable queryable) where T : class
        {
            var enumerator = queryable.GetEnumerator();

            while (enumerator.MoveNext())
            {
                yield return enumerator.Current as T;
            }
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
