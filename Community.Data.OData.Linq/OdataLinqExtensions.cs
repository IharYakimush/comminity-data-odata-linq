using Community.OData.Linq.Builder.Validators;
using Community.OData.Linq.Common;
using Community.OData.Linq.Properties;

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
        private static readonly FilterQueryValidator FilterValidator =
            new FilterQueryValidator(new DefaultQuerySettings {EnableFilter = true});

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

        public static ODataQuery<T> Filter<T>(this ODataQuery<T> query, string filterText, string entitySetName = null)
        {
            IEdmModel edmModel = query.EdmModel;

            ODataQueryOptionParser queryOptionParser = GetParser(query, entitySetName,
                new Dictionary<string, string> {{"$filter", filterText}});               

            ODataSettings settings = query.ServiceProvider.GetRequiredService<ODataSettings>();

            // Workaround for strange behavior in QueryOptionsParserConfiguration constructor which set it to false always
            queryOptionParser.Resolver.EnableCaseInsensitive = settings.EnableCaseInsensitive;

            FilterClause filterClause = queryOptionParser.ParseFilter();
            SingleValueNode filterExpression = filterClause.Expression.Accept(
                new ParameterAliasNodeTranslator(queryOptionParser.ParameterAliasNodes)) as SingleValueNode;
            filterExpression = filterExpression ?? new ConstantNode(null);
            filterClause = new FilterClause(filterExpression, filterClause.RangeVariable);
            Contract.Assert(filterClause != null);

            FilterValidator.Validate(filterClause, settings.ValidationSettings, edmModel);

            Expression filter = FilterBinder.Bind(query, filterClause, typeof(T), query.ServiceProvider);
            var result = ExpressionHelpers.Where(query, filter, typeof(T));

            return new ODataQuery<T>(result, query.ServiceProvider);
        }

        public static IOrderedQueryable<T> OrderBy<T>(this ODataQuery<T> query, string orderbyText, string entitySetName = null)
        {
            IEdmModel edmModel = query.EdmModel;
            
            ODataQueryOptionParser queryOptionParser = GetParser(query, entitySetName,
                    new Dictionary<string, string> { { "$orderby", orderbyText } });

            var orderByClause = queryOptionParser.ParseOrderBy();

            orderByClause = TranslateParameterAlias(orderByClause, queryOptionParser);

            ODataSettings settings = query.ServiceProvider.GetRequiredService<ODataSettings>();

            IOrderedQueryable<T> result = (IOrderedQueryable<T>) OrderApplyToCore<T>(query, settings.QuerySettings, orderByClause, edmModel);

            return new ODataQueryOrdered<T>(result,query.ServiceProvider);
        }

        private static IOrderedQueryable OrderApplyToCore<T>(ODataQuery<T> query, ODataQuerySettings querySettings, OrderByClause orderByClause, IEdmModel model)
        {            
            ICollection<OrderByNode> nodes = OrderByNode.CreateCollection(orderByClause);

            bool alreadyOrdered = false;
            IQueryable querySoFar = query;

            HashSet<object> propertiesSoFar = new HashSet<object>();
            HashSet<string> openPropertiesSoFar = new HashSet<string>();
            bool orderByItSeen = false;

            foreach (OrderByNode node in nodes)
            {
                OrderByPropertyNode propertyNode = node as OrderByPropertyNode;
                OrderByOpenPropertyNode openPropertyNode = node as OrderByOpenPropertyNode;

                if (propertyNode != null)
                {
                    // Use autonomy class to achieve value equality for HasSet.
                    var edmPropertyWithPath = new { propertyNode.Property, propertyNode.PropertyPath };
                    OrderByDirection direction = propertyNode.Direction;

                    // This check prevents queries with duplicate properties (e.g. $orderby=Id,Id,Id,Id...) from causing stack overflows
                    if (propertiesSoFar.Contains(edmPropertyWithPath))
                    {
                        throw new ODataException(Error.Format(SRResources.OrderByDuplicateProperty, edmPropertyWithPath.PropertyPath));
                    }

                    propertiesSoFar.Add(edmPropertyWithPath);

                    if (propertyNode.OrderByClause != null)
                    {
                        querySoFar = AddOrderByQueryForProperty(query, querySettings, propertyNode.OrderByClause, querySoFar, direction, alreadyOrdered);
                    }
                    else
                    {
                        querySoFar = ExpressionHelpers.OrderByProperty(querySoFar, model, edmPropertyWithPath.Property, direction, typeof(T), alreadyOrdered);
                    }

                    alreadyOrdered = true;
                }
                else if (openPropertyNode != null)
                {
                    // This check prevents queries with duplicate properties (e.g. $orderby=Id,Id,Id,Id...) from causing stack overflows
                    if (openPropertiesSoFar.Contains(openPropertyNode.PropertyName))
                    {
                        throw new ODataException(Error.Format(SRResources.OrderByDuplicateProperty, openPropertyNode.PropertyPath));
                    }

                    openPropertiesSoFar.Add(openPropertyNode.PropertyName);
                    Contract.Assert(openPropertyNode.OrderByClause != null);
                    querySoFar = AddOrderByQueryForProperty(query, querySettings, openPropertyNode.OrderByClause, querySoFar, openPropertyNode.Direction, alreadyOrdered);
                    alreadyOrdered = true;
                }
                else
                {
                    // This check prevents queries with duplicate nodes (e.g. $orderby=$it,$it,$it,$it...) from causing stack overflows
                    if (orderByItSeen)
                    {
                        throw new ODataException(Error.Format(SRResources.OrderByDuplicateIt));
                    }

                    querySoFar = ExpressionHelpers.OrderByIt(querySoFar, node.Direction, typeof(T), alreadyOrdered);
                    alreadyOrdered = true;
                    orderByItSeen = true;
                }
            }

            return querySoFar as IOrderedQueryable;
        }

        private static IQueryable AddOrderByQueryForProperty<T>(ODataQuery<T> query, ODataQuerySettings querySettings,
            OrderByClause orderbyClause, IQueryable querySoFar, OrderByDirection direction, bool alreadyOrdered)
        {
            //Context.UpdateQuerySettings(querySettings, query);

            LambdaExpression orderByExpression =
                FilterBinder.Bind(query, orderbyClause, typeof(T), query.ServiceProvider);
            querySoFar = ExpressionHelpers.OrderBy(querySoFar, orderByExpression, direction, typeof(T),
                alreadyOrdered);
            return querySoFar;
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

        private static ODataQueryOptionParser GetParser<T>(ODataQuery<T> query,string entitySetName, Dictionary<string, string> raws)
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

            return new ODataQueryOptionParser(
                edmModel,
                path,
                raws,
                query.ServiceProvider);
        }
    }
}
