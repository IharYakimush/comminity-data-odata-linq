// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Community.OData.Linq.Builder.Validators
{
    using Community.OData.Linq.Common;
    using Community.OData.Linq.OData.Formatter;
    using Community.OData.Linq.Properties;

    using Microsoft.OData;
    using Microsoft.OData.Edm;
    using Microsoft.OData.UriParser;

    internal class OrderByModelLimitationsValidator : QueryNodeVisitor<SingleValueNode>
    {
        private readonly IEdmModel _model;
        private readonly bool _enableOrderBy;
        private IEdmProperty _property;
        private IEdmStructuredType _structuredType;

        public OrderByModelLimitationsValidator(IEdmModel model, bool enableOrderBy)
        {
            this._model = model;
            this._enableOrderBy = enableOrderBy;            
        }

        public bool TryValidate(IEdmProperty property, IEdmStructuredType structuredType, OrderByClause orderByClause,
            bool explicitPropertiesDefined)
        {
            this._property = property;
            this._structuredType = structuredType;
            return this.TryValidate(orderByClause, explicitPropertiesDefined);
        }

        // Visits the expression to find the first node if any, that is not sortable and throws
        // an exception only if no explicit properties have been defined in AllowedOrderByProperties
        // on the ODataValidationSettings instance associated with this OrderByValidator.
        public bool TryValidate(OrderByClause orderByClause, bool explicitPropertiesDefined)
        {
            SingleValueNode invalidNode = orderByClause.Expression.Accept(this);
            if (invalidNode != null && !explicitPropertiesDefined)
            {
                throw new ODataException(Error.Format(SRResources.NotSortablePropertyUsedInOrderBy,
                    GetPropertyName(invalidNode)));
            }
            return invalidNode == null;
        }

        public override SingleValueNode Visit(SingleValuePropertyAccessNode nodeIn)
        {
            if (nodeIn.Source != null)
            {
                if (nodeIn.Source.Kind == QueryNodeKind.SingleNavigationNode)
                {
                    SingleNavigationNode singleNavigationNode = nodeIn.Source as SingleNavigationNode;
                    if (EdmLibHelpers.IsNotSortable(nodeIn.Property, singleNavigationNode.NavigationProperty,
                        singleNavigationNode.NavigationProperty.ToEntityType(), this._model, this._enableOrderBy))
                    {
                        return nodeIn;
                    }
                }
                else if (nodeIn.Source.Kind == QueryNodeKind.SingleComplexNode)
                {
                    SingleComplexNode singleComplexNode = nodeIn.Source as SingleComplexNode;
                    if (EdmLibHelpers.IsNotSortable(nodeIn.Property, singleComplexNode.Property,
                        nodeIn.Property.DeclaringType, this._model, this._enableOrderBy))
                    {
                        return nodeIn;
                    }
                }
                else if (EdmLibHelpers.IsNotSortable(nodeIn.Property, this._property, this._structuredType, this._model, this._enableOrderBy))
                {
                    return nodeIn;
                }
            }

            if (nodeIn.Source != null)
            {
                return nodeIn.Source.Accept(this);
            }

            return null;
        }

        public override SingleValueNode Visit(SingleComplexNode nodeIn)
        {
            if (EdmLibHelpers.IsNotSortable(nodeIn.Property, this._property, this._structuredType, this._model, this._enableOrderBy))
            {
                return nodeIn;
            }

            if (nodeIn.Source != null)
            {
                return nodeIn.Source.Accept(this);
            }

            return null;
        }

        public override SingleValueNode Visit(SingleNavigationNode nodeIn)
        {
            if (EdmLibHelpers.IsNotSortable(nodeIn.NavigationProperty, this._property, this._structuredType, this._model,
                this._enableOrderBy))
            {
                return nodeIn;
            }

            if (nodeIn.Source != null)
            {
                return nodeIn.Source.Accept(this);
            }

            return null;
        }

        public override SingleValueNode Visit(ResourceRangeVariableReferenceNode nodeIn)
        {
            return null;
        }

        public override SingleValueNode Visit(NonResourceRangeVariableReferenceNode nodeIn)
        {
            return null;
        }

        private static string GetPropertyName(SingleValueNode node)
        {
            if (node.Kind == QueryNodeKind.SingleNavigationNode)
            {
                return ((SingleNavigationNode)node).NavigationProperty.Name;
            }
            else if (node.Kind == QueryNodeKind.SingleValuePropertyAccess)
            {
                return ((SingleValuePropertyAccessNode)node).Property.Name;
            }
            else if (node.Kind == QueryNodeKind.SingleComplexNode)
            {
                return ((SingleComplexNode)node).Property.Name;
            }
            return null;
        }
    }
}
