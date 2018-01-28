namespace Community.OData.Linq.OData.Query.Expressions
{
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Linq.Expressions;

    using Community.OData.Linq.Common;
    using Community.OData.Linq.Properties;

    using Microsoft.OData;
    using Microsoft.OData.Edm;
    using Microsoft.OData.UriParser;

    public static class OrderByBinder
    {
        public static IOrderedQueryable OrderApplyToCore<T>(ODataQuery<T> query, ODataQuerySettings querySettings, ICollection<OrderByNode> nodes, IEdmModel model)
        {
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
    }
}