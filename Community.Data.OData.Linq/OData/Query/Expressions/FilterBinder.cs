﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Community.OData.Linq.OData.Query.Expressions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Runtime.CompilerServices;

    using Community.OData.Linq.Common;
    using Community.OData.Linq.OData.Formatter;
    using Community.OData.Linq.Properties;

    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.OData;
    using Microsoft.OData.Edm;
    using Microsoft.OData.UriParser;

    /// <summary>
    /// Translates an OData $filter parse tree represented by <see cref="FilterClause"/> to
    /// an <see cref="Expression"/> and applies it to an <see cref="IQueryable"/>.
    /// </summary>
    [SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "Relies on many ODataLib classes.")]
    public class FilterBinder : ExpressionBinderBase
    {
        private const string ODataItParameterName = "$it";

        private static readonly string _dictionaryStringObjectIndexerName = typeof(Dictionary<string, object>).GetDefaultMembers()[0].Name;

        private Stack<Dictionary<string, ParameterExpression>> _parametersStack = new Stack<Dictionary<string, ParameterExpression>>();
        private Dictionary<string, ParameterExpression> _lambdaParameters;
        private Type _filterType;

        /// <summary>
        /// Initializes a new instance of the <see cref="FilterBinder"/> class.
        /// </summary>
        /// <param name="requestContainer">The request container.</param>
        public FilterBinder(IServiceProvider requestContainer)
            : base(requestContainer)
        {
        }

        internal static Expression Bind(IQueryable baseQuery, FilterClause filterClause, Type filterType, IServiceProvider requestContainer)
        {
            if (filterClause == null)
            {
                throw Error.ArgumentNull("filterClause");
            }
            if (filterType == null)
            {
                throw Error.ArgumentNull("filterType");
            }
            if (requestContainer == null)
            {
                throw Error.ArgumentNull("requestContainer");
            }

            FilterBinder binder = requestContainer.GetRequiredService<FilterBinder>();
            binder._filterType = filterType;
            binder.BaseQuery = baseQuery;

            return BindFilterClause(binder, filterClause, filterType);
        }

        internal static LambdaExpression Bind(IQueryable baseQuery, OrderByClause orderBy, Type elementType, IServiceProvider requestContainer)
        {
            Contract.Assert(orderBy != null);
            Contract.Assert(elementType != null);
            Contract.Assert(requestContainer != null);

            FilterBinder binder = requestContainer.GetRequiredService<FilterBinder>();
            binder._filterType = elementType;
            binder.BaseQuery = baseQuery;

            return BindOrderByClause(binder, orderBy, elementType);
        }

        #region For testing purposes only.

        private FilterBinder(
            IEdmModel model,
            ODataQuerySettings querySettings,
            Type filterType)
            : base(model,  querySettings)
        {
            this._filterType = filterType;
        }

        internal static Expression<Func<TEntityType, bool>> Bind<TEntityType>(FilterClause filterClause, IEdmModel model,
             ODataQuerySettings querySettings)
        {
            return Bind(filterClause, typeof(TEntityType), model, querySettings) as Expression<Func<TEntityType, bool>>;
        }

        internal static Expression Bind(FilterClause filterClause, Type filterType, IEdmModel model,
             ODataQuerySettings querySettings)
        {
            if (filterClause == null)
            {
                throw Error.ArgumentNull("filterClause");
            }
            if (filterType == null)
            {
                throw Error.ArgumentNull("filterType");
            }
            if (model == null)
            {
                throw Error.ArgumentNull("model");
            }

            FilterBinder binder = new FilterBinder(model, querySettings, filterType);

            return BindFilterClause(binder, filterClause, filterType);
        }

        #endregion

        private static LambdaExpression BindFilterClause(FilterBinder binder, FilterClause filterClause, Type filterType)
        {
            LambdaExpression filter = binder.BindExpression(filterClause.Expression, filterClause.RangeVariable, filterType);
            filter = Expression.Lambda(binder.ApplyNullPropagationForFilterBody(filter.Body), filter.Parameters);

            Type expectedFilterType = typeof(Func<,>).MakeGenericType(filterType, typeof(bool));
            if (filter.Type != expectedFilterType)
            {
                throw Error.Argument("filterType", SRResources.CannotCastFilter, filter.Type.FullName, expectedFilterType.FullName);
            }

            return filter;
        }

        private static LambdaExpression BindOrderByClause(FilterBinder binder, OrderByClause orderBy, Type elementType)
        {
            LambdaExpression orderByLambda = binder.BindExpression(orderBy.Expression, orderBy.RangeVariable, elementType);
            return orderByLambda;
        }

        /// <summary>
        /// Binds a <see cref="QueryNode"/> to create a LINQ <see cref="Expression"/> that represents the semantics
        /// of the <see cref="QueryNode"/>.
        /// </summary>
        /// <param name="node">The node to bind.</param>
        /// <returns>The LINQ <see cref="Expression"/> created.</returns>
        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity",
            Justification = "These are simple conversion function and cannot be split up.")]
        public virtual Expression Bind(QueryNode node)
        {
            // Recursion guard to avoid stack overflows
            RuntimeHelpers.EnsureSufficientExecutionStack();

            CollectionNode collectionNode = node as CollectionNode;
            SingleValueNode singleValueNode = node as SingleValueNode;

            if (collectionNode != null)
            {
                switch (node.Kind)
                {
                    case QueryNodeKind.CollectionNavigationNode:
                        CollectionNavigationNode navigationNode = node as CollectionNavigationNode;
                        return this.BindNavigationPropertyNode(navigationNode.Source, navigationNode.NavigationProperty);

                    case QueryNodeKind.CollectionPropertyAccess:
                        return this.BindCollectionPropertyAccessNode(node as CollectionPropertyAccessNode);

                    case QueryNodeKind.CollectionComplexNode:
                        return this.BindCollectionComplexNode(node as CollectionComplexNode);

                    case QueryNodeKind.CollectionResourceCast:
                        return this.BindCollectionResourceCastNode(node as CollectionResourceCastNode);

                    case QueryNodeKind.CollectionFunctionCall:
                    case QueryNodeKind.CollectionResourceFunctionCall:
                    case QueryNodeKind.CollectionOpenPropertyAccess:
                    // Unused or have unknown uses.
                    default:
                        throw Error.NotSupported(SRResources.QueryNodeBindingNotSupported, node.Kind, typeof(FilterBinder).Name);
                }
            }
            else if (singleValueNode != null)
            {
                switch (node.Kind)
                {
                    case QueryNodeKind.BinaryOperator:
                        return this.BindBinaryOperatorNode(node as BinaryOperatorNode);

                    case QueryNodeKind.Constant:
                        return this.BindConstantNode(node as ConstantNode);

                    case QueryNodeKind.Convert:
                        return this.BindConvertNode(node as ConvertNode);

                    case QueryNodeKind.ResourceRangeVariableReference:
                        return this.BindRangeVariable((node as ResourceRangeVariableReferenceNode).RangeVariable);

                    case QueryNodeKind.NonResourceRangeVariableReference:
                        return this.BindRangeVariable((node as NonResourceRangeVariableReferenceNode).RangeVariable);

                    case QueryNodeKind.SingleValuePropertyAccess:
                        return this.BindPropertyAccessQueryNode(node as SingleValuePropertyAccessNode);

                    case QueryNodeKind.SingleComplexNode:
                        return this.BindSingleComplexNode(node as SingleComplexNode);

                    case QueryNodeKind.SingleValueOpenPropertyAccess:
                        return this.BindDynamicPropertyAccessQueryNode(node as SingleValueOpenPropertyAccessNode);

                    case QueryNodeKind.UnaryOperator:
                        return this.BindUnaryOperatorNode(node as UnaryOperatorNode);

                    case QueryNodeKind.SingleValueFunctionCall:
                        return this.BindSingleValueFunctionCallNode(node as SingleValueFunctionCallNode);

                    case QueryNodeKind.SingleNavigationNode:
                        SingleNavigationNode navigationNode = node as SingleNavigationNode;
                        return this.BindNavigationPropertyNode(navigationNode.Source, navigationNode.NavigationProperty);

                    case QueryNodeKind.Any:
                        return this.BindAnyNode(node as AnyNode);

                    case QueryNodeKind.All:
                        return this.BindAllNode(node as AllNode);

                    case QueryNodeKind.SingleResourceCast:
                        return this.BindSingleResourceCastNode(node as SingleResourceCastNode);

                    case QueryNodeKind.SingleResourceFunctionCall:
                        return this.BindSingleResourceFunctionCallNode(node as SingleResourceFunctionCallNode);

                    case QueryNodeKind.NamedFunctionParameter:
                    case QueryNodeKind.ParameterAlias:
                    case QueryNodeKind.EntitySet:
                    case QueryNodeKind.KeyLookup:
                    case QueryNodeKind.SearchTerm:
                    // Unused or have unknown uses.
                    default:
                        throw Error.NotSupported(SRResources.QueryNodeBindingNotSupported, node.Kind, typeof(FilterBinder).Name);
                }
            }
            else
            {
                throw Error.NotSupported(SRResources.QueryNodeBindingNotSupported, node.Kind, typeof(FilterBinder).Name);
            }
        }

        /// <summary>
        /// Binds a <see cref="SingleValueOpenPropertyAccessNode"/> to create a LINQ <see cref="Expression"/> that
        /// represents the semantics of the <see cref="SingleValueOpenPropertyAccessNode"/>.
        /// </summary>
        /// <param name="openNode">The node to bind.</param>
        /// <returns>The LINQ <see cref="Expression"/> created.</returns>
        public virtual Expression BindDynamicPropertyAccessQueryNode(SingleValueOpenPropertyAccessNode openNode)
        {
            if (EdmLibHelpers.IsDynamicTypeWrapper(this._filterType))
            {
                return this.GetFlattenedPropertyExpression(openNode.Name) ?? Expression.Property(this.Bind(openNode.Source), openNode.Name);
            }
            PropertyInfo prop = this.GetDynamicPropertyContainer(openNode);

            var propertyAccessExpression = this.BindPropertyAccessExpression(openNode, prop);
            var readDictionaryIndexerExpression = Expression.Property(propertyAccessExpression,
                _dictionaryStringObjectIndexerName, Expression.Constant(openNode.Name));
            var containsKeyExpression = Expression.Call(propertyAccessExpression,
                propertyAccessExpression.Type.GetMethod("ContainsKey"), Expression.Constant(openNode.Name));
            var nullExpression = Expression.Constant(null);

            if (this.QuerySettings.HandleNullPropagation == HandleNullPropagationOption.True)
            {
                var dynamicDictIsNotNull = Expression.NotEqual(propertyAccessExpression, Expression.Constant(null));
                var dynamicDictIsNotNullAndContainsKey = Expression.AndAlso(dynamicDictIsNotNull, containsKeyExpression);
                return Expression.Condition(
                    dynamicDictIsNotNullAndContainsKey,
                    readDictionaryIndexerExpression,
                    nullExpression);
            }
            else
            {
                return Expression.Condition(
                    containsKeyExpression,
                    readDictionaryIndexerExpression,
                    nullExpression);
            }
        }

        private Expression BindPropertyAccessExpression(SingleValueOpenPropertyAccessNode openNode, PropertyInfo prop)
        {
            var source = this.Bind(openNode.Source);
            Expression propertyAccessExpression;
            if (this.QuerySettings.HandleNullPropagation == HandleNullPropagationOption.True &&
                IsNullable(source.Type) && source != this._lambdaParameters[ODataItParameterName])
            {
                propertyAccessExpression = Expression.Property(this.RemoveInnerNullPropagation(source), prop.Name);
            }
            else
            {
                propertyAccessExpression = Expression.Property(source, prop.Name);
            }
            return propertyAccessExpression;
        }

        private PropertyInfo GetDynamicPropertyContainer(SingleValueOpenPropertyAccessNode openNode)
        {
            IEdmStructuredType edmStructuredType;
            var edmTypeReference = openNode.Source.TypeReference;
            if (edmTypeReference.IsEntity())
            {
                edmStructuredType = edmTypeReference.AsEntity().EntityDefinition();
            }
            else if (edmTypeReference.IsComplex())
            {
                edmStructuredType = edmTypeReference.AsComplex().ComplexDefinition();
            }
            else
            {
                throw Error.NotSupported(SRResources.QueryNodeBindingNotSupported, openNode.Kind, typeof(FilterBinder).Name);
            }
            var prop = EdmLibHelpers.GetDynamicPropertyDictionary(edmStructuredType, this.Model);
            return prop;
        }

        /// <summary>
        /// Binds a <see cref="SingleResourceFunctionCallNode"/> to create a LINQ <see cref="Expression"/> that
        /// represents the semantics of the <see cref="SingleResourceFunctionCallNode"/>.
        /// </summary>
        /// <param name="node">The node to bind.</param>
        /// <returns>The LINQ <see cref="Expression"/> created.</returns>
        public virtual Expression BindSingleResourceFunctionCallNode(SingleResourceFunctionCallNode node)
        {
            switch (node.Name)
            {
                case ClrCanonicalFunctions.CastFunctionName:
                    return this.BindSingleResourceCastFunctionCall(node);
                default:
                    throw Error.NotSupported(SRResources.ODataFunctionNotSupported, node.Name);
            }
        }

        private Expression BindSingleResourceCastFunctionCall(SingleResourceFunctionCallNode node)
        {
            Contract.Assert(ClrCanonicalFunctions.CastFunctionName == node.Name);

            Expression[] arguments = this.BindArguments(node.Parameters);

            Contract.Assert(arguments.Length == 2);

            string targetEdmTypeName = (string)((ConstantNode)node.Parameters.Last()).Value;
            IEdmType targetEdmType = this.Model.FindType(targetEdmTypeName);
            Type targetClrType = null;

            if (targetEdmType != null)
            {
                targetClrType = EdmLibHelpers.GetClrType(targetEdmType.ToEdmTypeReference(false), this.Model);
            }

            if (arguments[0].Type == targetClrType)
            {
                // We only support to cast Entity type to the same type now.
                return arguments[0];
            }
            else
            {
                // Cast fails and return null.
                return NullConstant;
            }
        }

        /// <summary>
        /// Binds a <see cref="SingleResourceCastNode"/> to create a LINQ <see cref="Expression"/> that
        /// represents the semantics of the <see cref="SingleResourceCastNode"/>.
        /// </summary>
        /// <param name="node">The node to bind.</param>
        /// <returns>The LINQ <see cref="Expression"/> created.</returns>
        public virtual Expression BindSingleResourceCastNode(SingleResourceCastNode node)
        {
            IEdmStructuredTypeReference structured = node.StructuredTypeReference;
            Contract.Assert(structured != null, "NS casts can contain only structured types");

            Type clrType = EdmLibHelpers.GetClrType(structured, this.Model);

            Expression source = this.BindCastSourceNode(node.Source);
            return Expression.TypeAs(source, clrType);
        }

        /// <summary>
        /// Binds a <see cref="CollectionResourceCastNode"/> to create a LINQ <see cref="Expression"/> that
        /// represents the semantics of the <see cref="CollectionResourceCastNode"/>.
        /// </summary>
        /// <param name="node">The node to bind.</param>
        /// <returns>The LINQ <see cref="Expression"/> created.</returns>
        public virtual Expression BindCollectionResourceCastNode(CollectionResourceCastNode node)
        {
            IEdmStructuredTypeReference structured = node.ItemStructuredType;
            Contract.Assert(structured != null, "NS casts can contain only structured types");

            Type clrType = EdmLibHelpers.GetClrType(structured, this.Model);

            Expression source = this.BindCastSourceNode(node.Source);
            return OfType(source, clrType);
        }

        private Expression BindCastSourceNode(QueryNode sourceNode)
        {
            Expression source;
            if (sourceNode == null)
            {
                // if the cast is on the root i.e $it (~/Products?$filter=NS.PopularProducts/.....),
                // source would be null. So bind null to '$it'.
                source = this._lambdaParameters[ODataItParameterName];
            }
            else
            {
                source = this.Bind(sourceNode);
            }

            return source;
        }

        private static Expression OfType(Expression source, Type elementType)
        {
            Contract.Assert(source != null);
            Contract.Assert(elementType != null);

            if (IsIQueryable(source.Type))
            {
                return Expression.Call(null, ExpressionHelperMethods.QueryableOfType.MakeGenericMethod(elementType), source);
            }
            else
            {
                return Expression.Call(null, ExpressionHelperMethods.EnumerableOfType.MakeGenericMethod(elementType), source);
            }
        }

        /// <summary>
        /// Binds a <see cref="IEdmNavigationProperty"/> to create a LINQ <see cref="Expression"/> that
        /// represents the semantics of the <see cref="IEdmNavigationProperty"/>.
        /// </summary>
        /// <param name="sourceNode">The node that represents the navigation source.</param>
        /// <param name="navigationProperty">The navigation property to bind.</param>
        /// <returns>The LINQ <see cref="Expression"/> created.</returns>
        public virtual Expression BindNavigationPropertyNode(QueryNode sourceNode, IEdmNavigationProperty navigationProperty)
        {
            Expression source;

            // TODO: bug in uri parser is causing this property to be null for the root property.
            if (sourceNode == null)
            {
                source = this._lambdaParameters[ODataItParameterName];
            }
            else
            {
                source = this.Bind(sourceNode);
            }

            return this.CreatePropertyAccessExpression(source, navigationProperty);
        }

        /// <summary>
        /// Binds a <see cref="BinaryOperatorNode"/> to create a LINQ <see cref="Expression"/> that
        /// represents the semantics of the <see cref="BinaryOperatorNode"/>.
        /// </summary>
        /// <param name="binaryOperatorNode">The node to bind.</param>
        /// <returns>The LINQ <see cref="Expression"/> created.</returns>
        public virtual Expression BindBinaryOperatorNode(BinaryOperatorNode binaryOperatorNode)
        {
            Expression left = this.Bind(binaryOperatorNode.Left);
            Expression right = this.Bind(binaryOperatorNode.Right);

            // handle null propagation only if either of the operands can be null
            bool isNullPropagationRequired = this.QuerySettings.HandleNullPropagation == HandleNullPropagationOption.True && (IsNullable(left.Type) || IsNullable(right.Type));
            if (isNullPropagationRequired)
            {
                // |----------------------------------------------------------------|
                // |SQL 3VL truth table.                                            |
                // |----------------------------------------------------------------|
                // |p       |    q      |    p OR q     |    p AND q    |    p = q  |
                // |----------------------------------------------------------------|
                // |True    |   True    |   True        |   True        |   True    |
                // |True    |   False   |   True        |   False       |   False   |
                // |True    |   NULL    |   True        |   NULL        |   NULL    |
                // |False   |   True    |   True        |   False       |   False   |
                // |False   |   False   |   False       |   False       |   True    |
                // |False   |   NULL    |   NULL        |   False       |   NULL    |
                // |NULL    |   True    |   True        |   NULL        |   NULL    |
                // |NULL    |   False   |   NULL        |   False       |   NULL    |
                // |NULL    |   NULL    |   Null        |   NULL        |   NULL    |
                // |--------|-----------|---------------|---------------|-----------|

                // before we start with null propagation, convert the operators to nullable if already not.
                left = ToNullable(left);
                right = ToNullable(right);

                bool liftToNull = true;
                if (left == NullConstant || right == NullConstant)
                {
                    liftToNull = false;
                }

                // Expression trees do a very good job of handling the 3VL truth table if we pass liftToNull true.
                return this.CreateBinaryExpression(binaryOperatorNode.OperatorKind, left, right, liftToNull: liftToNull);
            }
            else
            {
                return this.CreateBinaryExpression(binaryOperatorNode.OperatorKind, left, right, liftToNull: false);
            }
        }

        /// <summary>
        /// Binds a <see cref="ConstantNode"/> to create a LINQ <see cref="Expression"/> that
        /// represents the semantics of the <see cref="ConstantNode"/>.
        /// </summary>
        /// <param name="constantNode">The node to bind.</param>
        /// <returns>The LINQ <see cref="Expression"/> created.</returns>
        public virtual Expression BindConstantNode(ConstantNode constantNode)
        {
            Contract.Assert(constantNode != null);

            // no need to parameterize null's as there cannot be multiple values for null.
            if (constantNode.Value == null)
            {
                return NullConstant;
            }

            Type constantType = EdmLibHelpers.GetClrType(constantNode.TypeReference, this.Model);
            object value = constantNode.Value;

            if (constantNode.TypeReference != null && constantNode.TypeReference.IsEnum())
            {
                ODataEnumValue odataEnumValue = (ODataEnumValue)value;
                string strValue = odataEnumValue.Value;
                Contract.Assert(strValue != null);

                constantType = Nullable.GetUnderlyingType(constantType) ?? constantType;
                value = Enum.Parse(constantType, strValue);
            }

            if (constantNode.TypeReference != null &&
                constantNode.TypeReference.IsNullable &&
                (constantNode.TypeReference.IsDate() || constantNode.TypeReference.IsTimeOfDay()))
            {
                constantType = Nullable.GetUnderlyingType(constantType) ?? constantType;
            }

            if (this.QuerySettings.EnableConstantParameterization)
            {
                return LinqParameterContainer.Parameterize(constantType, value);
            }
            else
            {
                return Expression.Constant(value, constantType);
            }
        }

        /// <summary>
        /// Binds a <see cref="ConvertNode"/> to create a LINQ <see cref="Expression"/> that
        /// represents the semantics of the <see cref="ConvertNode"/>.
        /// </summary>
        /// <param name="convertNode">The node to bind.</param>
        /// <returns>The LINQ <see cref="Expression"/> created.</returns>
        public virtual Expression BindConvertNode(ConvertNode convertNode)
        {
            Contract.Assert(convertNode != null);
            Contract.Assert(convertNode.TypeReference != null);

            Expression source = this.Bind(convertNode.Source);

            return this.CreateConvertExpression(convertNode, source);
        }

        private LambdaExpression BindExpression(SingleValueNode expression, RangeVariable rangeVariable, Type elementType)
        {
            ParameterExpression filterParameter = Expression.Parameter(elementType, rangeVariable.Name);
            this._lambdaParameters = new Dictionary<string, ParameterExpression>();
            this._lambdaParameters.Add(rangeVariable.Name, filterParameter);

            this.EnsureFlattenedPropertyContainer(filterParameter);

            Expression body = this.Bind(expression);
            return Expression.Lambda(body, filterParameter);
        }

        private Expression ApplyNullPropagationForFilterBody(Expression body)
        {
            if (IsNullable(body.Type))
            {
                if (this.QuerySettings.HandleNullPropagation == HandleNullPropagationOption.True)
                {
                    // handle null as false
                    // body => body == true. passing liftToNull:false would convert null to false.
                    body = Expression.Equal(body, Expression.Constant(true, typeof(bool?)), liftToNull: false, method: null);
                }
                else
                {
                    body = Expression.Convert(body, typeof(bool));
                }
            }

            return body;
        }

        /// <summary>
        /// Binds a <see cref="RangeVariable"/> to create a LINQ <see cref="Expression"/> that
        /// represents the semantics of the <see cref="RangeVariable"/>.
        /// </summary>
        /// <param name="rangeVariable">The range variable to bind.</param>
        /// <returns>The LINQ <see cref="Expression"/> created.</returns>
        public virtual Expression BindRangeVariable(RangeVariable rangeVariable)
        {
            ParameterExpression parameter = this._lambdaParameters[rangeVariable.Name];
            return this.ConvertNonStandardPrimitives(parameter);
        }

        /// <summary>
        /// Binds a <see cref="CollectionPropertyAccessNode"/> to create a LINQ <see cref="Expression"/> that
        /// represents the semantics of the <see cref="CollectionPropertyAccessNode"/>.
        /// </summary>
        /// <param name="propertyAccessNode">The node to bind.</param>
        /// <returns>The LINQ <see cref="Expression"/> created.</returns>
        public virtual Expression BindCollectionPropertyAccessNode(CollectionPropertyAccessNode propertyAccessNode)
        {
            Expression source = this.Bind(propertyAccessNode.Source);
            return this.CreatePropertyAccessExpression(source, propertyAccessNode.Property);
        }

        /// <summary>
        /// Binds a <see cref="CollectionComplexNode"/> to create a LINQ <see cref="Expression"/> that
        /// represents the semantics of the <see cref="CollectionComplexNode"/>.
        /// </summary>
        /// <param name="collectionComplexNode">The node to bind.</param>
        /// <returns>The LINQ <see cref="Expression"/> created.</returns>
        public virtual Expression BindCollectionComplexNode(CollectionComplexNode collectionComplexNode)
        {
            Expression source = this.Bind(collectionComplexNode.Source);
            return this.CreatePropertyAccessExpression(source, collectionComplexNode.Property);
        }

        /// <summary>
        /// Binds a <see cref="SingleValuePropertyAccessNode"/> to create a LINQ <see cref="Expression"/> that
        /// represents the semantics of the <see cref="SingleValuePropertyAccessNode"/>.
        /// </summary>
        /// <param name="propertyAccessNode">The node to bind.</param>
        /// <returns>The LINQ <see cref="Expression"/> created.</returns>
        public virtual Expression BindPropertyAccessQueryNode(SingleValuePropertyAccessNode propertyAccessNode)
        {
            Expression source = this.Bind(propertyAccessNode.Source);
            return this.CreatePropertyAccessExpression(source, propertyAccessNode.Property, this.GetFullPropertyPath(propertyAccessNode));
        }

        /// <summary>
        /// Binds a <see cref="SingleComplexNode"/> to create a LINQ <see cref="Expression"/> that
        /// represents the semantics of the <see cref="SingleComplexNode"/>.
        /// </summary>
        /// <param name="singleComplexNode">The node to bind.</param>
        /// <returns>The LINQ <see cref="Expression"/> created.</returns>
        public virtual Expression BindSingleComplexNode(SingleComplexNode singleComplexNode)
        {
            Expression source = this.Bind(singleComplexNode.Source);
            return this.CreatePropertyAccessExpression(source, singleComplexNode.Property, this.GetFullPropertyPath(singleComplexNode));
        }

        private Expression CreatePropertyAccessExpression(Expression source, IEdmProperty property, string propertyPath = null)
        {
            string propertyName = EdmLibHelpers.GetClrPropertyName(property, this.Model);
            propertyPath = propertyPath ?? propertyName;

            if (this.QuerySettings.HandleNullPropagation == HandleNullPropagationOption.True && IsNullable(source.Type) && source != this._lambdaParameters[ODataItParameterName])
            {
                var cleanSource = this.RemoveInnerNullPropagation(source);
                Expression propertyAccessExpression = null;

                propertyAccessExpression = this.GetFlattenedPropertyExpression(propertyPath) ?? Expression.Property(cleanSource, propertyName);

                // source.property => source == null ? null : [CastToNullable]RemoveInnerNullPropagation(source).property
                // Notice that we are checking if source is null already. so we can safely remove any null checks when doing source.Property

                Expression ifFalse = ToNullable(this.ConvertNonStandardPrimitives(propertyAccessExpression));
                return
                    Expression.Condition(
                        test: Expression.Equal(source, NullConstant),
                        ifTrue: Expression.Constant(null, ifFalse.Type),
                        ifFalse: ifFalse);
            }
            else
            {
                return this.GetFlattenedPropertyExpression(propertyPath) ?? this.ConvertNonStandardPrimitives(Expression.Property(source, propertyName));
            }
        }

        /// <summary>
        /// Binds a <see cref="UnaryOperatorNode"/> to create a LINQ <see cref="Expression"/> that
        /// represents the semantics of the <see cref="UnaryOperatorNode"/>.
        /// </summary>
        /// <param name="unaryOperatorNode">The node to bind.</param>
        /// <returns>The LINQ <see cref="Expression"/> created.</returns>
        public virtual Expression BindUnaryOperatorNode(UnaryOperatorNode unaryOperatorNode)
        {
            // No need to handle null-propagation here as CLR already handles it.
            // !(null) = null
            // -(null) = null
            Expression inner = this.Bind(unaryOperatorNode.Operand);
            switch (unaryOperatorNode.OperatorKind)
            {
                case UnaryOperatorKind.Negate:
                    return Expression.Negate(inner);

                case UnaryOperatorKind.Not:
                    return Expression.Not(inner);

                default:
                    throw Error.NotSupported(SRResources.QueryNodeBindingNotSupported, unaryOperatorNode.Kind, typeof(FilterBinder).Name);
            }
        }

        /// <summary>
        /// Binds a <see cref="SingleValueFunctionCallNode"/> to create a LINQ <see cref="Expression"/> that
        /// represents the semantics of the <see cref="SingleValueFunctionCallNode"/>.
        /// </summary>
        /// <param name="node">The node to bind.</param>
        /// <returns>The LINQ <see cref="Expression"/> created.</returns>
        public virtual Expression BindSingleValueFunctionCallNode(SingleValueFunctionCallNode node)
        {
            switch (node.Name)
            {
                case ClrCanonicalFunctions.StartswithFunctionName:
                    return this.BindStartsWith(node);

                case ClrCanonicalFunctions.EndswithFunctionName:
                    return this.BindEndsWith(node);

                case ClrCanonicalFunctions.ContainsFunctionName:
                    return this.BindContains(node);

                case ClrCanonicalFunctions.SubstringFunctionName:
                    return this.BindSubstring(node);

                case ClrCanonicalFunctions.LengthFunctionName:
                    return this.BindLength(node);

                case ClrCanonicalFunctions.IndexofFunctionName:
                    return this.BindIndexOf(node);

                case ClrCanonicalFunctions.TolowerFunctionName:
                    return this.BindToLower(node);

                case ClrCanonicalFunctions.ToupperFunctionName:
                    return this.BindToUpper(node);

                case ClrCanonicalFunctions.TrimFunctionName:
                    return this.BindTrim(node);

                case ClrCanonicalFunctions.ConcatFunctionName:
                    return this.BindConcat(node);

                case ClrCanonicalFunctions.YearFunctionName:
                case ClrCanonicalFunctions.MonthFunctionName:
                case ClrCanonicalFunctions.DayFunctionName:
                    return this.BindDateRelatedProperty(node); // Date & DateTime & DateTimeOffset

                case ClrCanonicalFunctions.HourFunctionName:
                case ClrCanonicalFunctions.MinuteFunctionName:
                case ClrCanonicalFunctions.SecondFunctionName:
                    return this.BindTimeRelatedProperty(node); // TimeOfDay & DateTime & DateTimeOffset

                case ClrCanonicalFunctions.FractionalSecondsFunctionName:
                    return this.BindFractionalSeconds(node);

                case ClrCanonicalFunctions.RoundFunctionName:
                    return this.BindRound(node);

                case ClrCanonicalFunctions.FloorFunctionName:
                    return this.BindFloor(node);

                case ClrCanonicalFunctions.CeilingFunctionName:
                    return this.BindCeiling(node);

                case ClrCanonicalFunctions.CastFunctionName:
                    return this.BindCastSingleValue(node);

                case ClrCanonicalFunctions.IsofFunctionName:
                    return this.BindIsOf(node);

                case ClrCanonicalFunctions.DateFunctionName:
                    return this.BindDate(node);

                case ClrCanonicalFunctions.TimeFunctionName:
                    return this.BindTime(node);

                default:
                    // Get Expression of custom binded method.
                    Expression expression = this.BindCustomMethodExpressionOrNull(node);
                    if (expression != null)
                    {
                        return expression;
                    }

                    throw new NotImplementedException(Error.Format(SRResources.ODataFunctionNotSupported, node.Name));
            }
        }

        private Expression BindCastSingleValue(SingleValueFunctionCallNode node)
        {
            Contract.Assert(ClrCanonicalFunctions.CastFunctionName == node.Name);

            Expression[] arguments = this.BindArguments(node.Parameters);
            Contract.Assert(arguments.Length == 1 || arguments.Length == 2);

            Expression source = arguments.Length == 1 ? this._lambdaParameters[ODataItParameterName] : arguments[0];
            string targetTypeName = (string)((ConstantNode)node.Parameters.Last()).Value;
            IEdmType targetEdmType = this.Model.FindType(targetTypeName);
            Type targetClrType = null;

            if (targetEdmType != null)
            {
                IEdmTypeReference targetEdmTypeReference = targetEdmType.ToEdmTypeReference(false);
                targetClrType = EdmLibHelpers.GetClrType(targetEdmTypeReference, this.Model);

                if (source != NullConstant)
                {
                    if (source.Type == targetClrType)
                    {
                        return source;
                    }

                    if ((!targetEdmTypeReference.IsPrimitive() && !targetEdmTypeReference.IsEnum()) ||
                        (EdmLibHelpers.GetEdmPrimitiveTypeOrNull(source.Type) == null && !TypeHelper.IsEnum(source.Type)))
                    {
                        // Cast fails and return null.
                        return NullConstant;
                    }
                }
            }

            if (targetClrType == null || source == NullConstant)
            {
                return NullConstant;
            }

            if (targetClrType == typeof(string))
            {
                return BindCastToStringType(source);
            }
            else if (TypeHelper.IsEnum(targetClrType))
            {
                return this.BindCastToEnumType(source.Type, targetClrType, node.Parameters.First(), arguments.Length);
            }
            else
            {
                if (source.Type.IsNullable() && !targetClrType.IsNullable())
                {
                    // Make the target Clr type nullable to avoid failure while casting
                    // nullable source, whose value may be null, to a non-nullable type.
                    // For example: cast(NullableInt32Property,Edm.Int64)
                    // The target Clr type should be Nullable<Int64> rather than Int64.
                    targetClrType = typeof(Nullable<>).MakeGenericType(targetClrType);
                }

                try
                {
                    return Expression.Convert(source, targetClrType);
                }
                catch (InvalidOperationException)
                {
                    // Cast fails and return null.
                    return NullConstant;
                }
            }
        }

        private static Expression BindCastToStringType(Expression source)
        {
            Expression sourceValue;

            if (source.Type.IsGenericType && source.Type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                if (TypeHelper.IsEnum(source.Type))
                {
                    // Entity Framework doesn't have ToString method for enum types.
                    // Convert enum types to their underlying numeric types.
                    sourceValue = Expression.Convert(
                        Expression.Property(source, "Value"),
                        Enum.GetUnderlyingType(TypeHelper.GetUnderlyingTypeOrSelf(source.Type)));
                }
                else
                {
                    // Entity Framework has ToString method for numeric types.
                    sourceValue = Expression.Property(source, "Value");
                }

                // Entity Framework doesn't have ToString method for nullable numeric types.
                // Call ToString method on non-nullable numeric types.
                return Expression.Condition(
                    Expression.Property(source, "HasValue"),
                    Expression.Call(sourceValue, "ToString", typeArguments: null, arguments: null),
                    Expression.Constant(null, typeof(string)));
            }
            else
            {
                sourceValue = TypeHelper.IsEnum(source.Type) ?
                    Expression.Convert(source, Enum.GetUnderlyingType(source.Type)) :
                    source;
                return Expression.Call(sourceValue, "ToString", typeArguments: null, arguments: null);
            }
        }

        private Expression BindCastToEnumType(Type sourceType, Type targetClrType, QueryNode firstParameter, int parameterLength)
        {
            Type enumType = TypeHelper.GetUnderlyingTypeOrSelf(targetClrType);
            ConstantNode sourceNode = firstParameter as ConstantNode;

            if (parameterLength == 1 || sourceNode == null || sourceType != typeof(string))
            {
                // We only support to cast Enumeration type from constant string now,
                // because LINQ to Entities does not recognize the method Enum.TryParse.
                return NullConstant;
            }
            else
            {
                object[] parameters = new[] { sourceNode.Value, Enum.ToObject(enumType, 0) };
                bool isSuccessful = (bool)EnumTryParseMethod.MakeGenericMethod(enumType).Invoke(null, parameters);

                if (isSuccessful)
                {
                    if (this.QuerySettings.EnableConstantParameterization)
                    {
                        return LinqParameterContainer.Parameterize(targetClrType, parameters[1]);
                    }
                    else
                    {
                        return Expression.Constant(parameters[1], targetClrType);
                    }
                }
                else
                {
                    return NullConstant;
                }
            }
        }

        private Expression BindIsOf(SingleValueFunctionCallNode node)
        {
            Contract.Assert(ClrCanonicalFunctions.IsofFunctionName == node.Name);

            Expression[] arguments = this.BindArguments(node.Parameters);

            // Edm.Boolean isof(type)  or
            // Edm.Boolean isof(expression,type)
            Contract.Assert(arguments.Length == 1 || arguments.Length == 2);

            Expression source = arguments.Length == 1 ? this._lambdaParameters[ODataItParameterName] : arguments[0];
            if (source == NullConstant)
            {
                return FalseConstant;
            }

            string typeName = (string)((ConstantNode)node.Parameters.Last()).Value;

            IEdmType edmType = this.Model.FindType(typeName);
            Type clrType = null;
            if (edmType != null)
            {
                // bool nullable = source.Type.IsNullable();
                IEdmTypeReference edmTypeReference = edmType.ToEdmTypeReference(false);
                clrType = EdmLibHelpers.GetClrType(edmTypeReference, this.Model);
            }

            if (clrType == null)
            {
                return FalseConstant;
            }

            bool isSourcePrimitiveOrEnum = EdmLibHelpers.GetEdmPrimitiveTypeOrNull(source.Type) != null ||
                                           TypeHelper.IsEnum(source.Type);

            bool isTargetPrimitiveOrEnum = EdmLibHelpers.GetEdmPrimitiveTypeOrNull(clrType) != null ||
                                           TypeHelper.IsEnum(clrType);

            if (isSourcePrimitiveOrEnum && isTargetPrimitiveOrEnum)
            {
                if (source.Type.IsNullable())
                {
                    clrType = clrType.ToNullable();
                }
            }

            // Be caution: Type method of LINQ to Entities only supports entity type.
            return Expression.Condition(Expression.TypeIs(source, clrType), TrueConstant, FalseConstant);
        }

        private Expression BindCeiling(SingleValueFunctionCallNode node)
        {
            Contract.Assert("ceiling" == node.Name);

            Expression[] arguments = this.BindArguments(node.Parameters);

            Contract.Assert(arguments.Length == 1 && IsDoubleOrDecimal(arguments[0].Type));

            MethodInfo ceiling = IsType<double>(arguments[0].Type)
                ? ClrCanonicalFunctions.CeilingOfDouble
                : ClrCanonicalFunctions.CeilingOfDecimal;
            return this.MakeFunctionCall(ceiling, arguments);
        }

        private Expression BindFloor(SingleValueFunctionCallNode node)
        {
            Contract.Assert("floor" == node.Name);

            Expression[] arguments = this.BindArguments(node.Parameters);

            Contract.Assert(arguments.Length == 1 && IsDoubleOrDecimal(arguments[0].Type));

            MethodInfo floor = IsType<double>(arguments[0].Type)
                ? ClrCanonicalFunctions.FloorOfDouble
                : ClrCanonicalFunctions.FloorOfDecimal;
            return this.MakeFunctionCall(floor, arguments);
        }

        private Expression BindRound(SingleValueFunctionCallNode node)
        {
            Contract.Assert("round" == node.Name);

            Expression[] arguments = this.BindArguments(node.Parameters);

            Contract.Assert(arguments.Length == 1 && IsDoubleOrDecimal(arguments[0].Type));

            MethodInfo round = IsType<double>(arguments[0].Type)
                ? ClrCanonicalFunctions.RoundOfDouble
                : ClrCanonicalFunctions.RoundOfDecimal;
            return this.MakeFunctionCall(round, arguments);
        }

        private Expression BindDate(SingleValueFunctionCallNode node)
        {
            Contract.Assert("date" == node.Name);

            Expression[] arguments = this.BindArguments(node.Parameters);

            // We should support DateTime & DateTimeOffset even though DateTime is not part of OData v4 Spec.
            Contract.Assert(arguments.Length == 1 && IsDateOrOffset(arguments[0].Type));

            // EF doesn't support new Date(int, int, int), also doesn't support other property access, for example DateTime.Date.
            // Therefore, we just return the source (DateTime or DateTimeOffset).
            return arguments[0];
        }

        private Expression BindTime(SingleValueFunctionCallNode node)
        {
            Contract.Assert("time" == node.Name);

            Expression[] arguments = this.BindArguments(node.Parameters);

            // We should support DateTime & DateTimeOffset even though DateTime is not part of OData v4 Spec.
            Contract.Assert(arguments.Length == 1 && IsDateOrOffset(arguments[0].Type));

            // EF doesn't support new TimeOfDay(int, int, int, int), also doesn't support other property access, for example DateTimeOffset.DateTime.
            // Therefore, we just return the source (DateTime or DateTimeOffset).
            return arguments[0];
        }

        private Expression BindFractionalSeconds(SingleValueFunctionCallNode node)
        {
            Contract.Assert("fractionalseconds" == node.Name);

            Expression[] arguments = this.BindArguments(node.Parameters);
            Contract.Assert(arguments.Length == 1 && (IsTimeRelated(arguments[0].Type)));

            // We should support DateTime & DateTimeOffset even though DateTime is not part of OData v4 Spec.
            Expression parameter = arguments[0];

            PropertyInfo property;
            if (IsTimeOfDay(parameter.Type))
            {
                property = ClrCanonicalFunctions.TimeOfDayProperties[ClrCanonicalFunctions.MillisecondFunctionName];
            }
            else if (IsDateTime(parameter.Type))
            {
                property = ClrCanonicalFunctions.DateTimeProperties[ClrCanonicalFunctions.MillisecondFunctionName];
            }
#if NET6_0_OR_GREATER
            else if (IsTimeOnly(parameter.Type))
            {
                property = ClrCanonicalFunctions.TimeOnlyProperties[ClrCanonicalFunctions.MillisecondFunctionName];
            }
#endif
            else if (IsTimeSpan(parameter.Type))
            {
                property = ClrCanonicalFunctions.TimeSpanProperties[ClrCanonicalFunctions.MillisecondFunctionName];
            }
            else
            {
                property = ClrCanonicalFunctions.DateTimeOffsetProperties[ClrCanonicalFunctions.MillisecondFunctionName];
            }

            // Millisecond
            Expression milliSecond = this.MakePropertyAccess(property, parameter);
            Expression decimalMilliSecond = Expression.Convert(milliSecond, typeof(decimal));
            Expression fractionalSeconds = Expression.Divide(decimalMilliSecond, Expression.Constant(1000m, typeof(decimal)));

            return this.CreateFunctionCallWithNullPropagation(fractionalSeconds, arguments);
        }

        private Expression BindDateRelatedProperty(SingleValueFunctionCallNode node)
        {
            Expression[] arguments = this.BindArguments(node.Parameters);
            Contract.Assert(arguments.Length == 1 && IsDateRelated(arguments[0].Type));

            // We should support DateTime & DateTimeOffset even though DateTime is not part of OData v4 Spec.
            Expression parameter = arguments[0];

            PropertyInfo property;
            if (IsDate(parameter.Type))
            {
                Contract.Assert(ClrCanonicalFunctions.DateProperties.ContainsKey(node.Name));
                property = ClrCanonicalFunctions.DateProperties[node.Name];
            }
            else if (IsDateTime(parameter.Type))
            {
                Contract.Assert(ClrCanonicalFunctions.DateTimeProperties.ContainsKey(node.Name));
                property = ClrCanonicalFunctions.DateTimeProperties[node.Name];
            }
#if NET6_0_OR_GREATER
            else if (IsDateOnly(parameter.Type))
            {
                Contract.Assert(ClrCanonicalFunctions.DateOnlyProperties.ContainsKey(node.Name));
                property = ClrCanonicalFunctions.DateOnlyProperties[node.Name];
            }
#endif
            else
            {
                Contract.Assert(ClrCanonicalFunctions.DateTimeOffsetProperties.ContainsKey(node.Name));
                property = ClrCanonicalFunctions.DateTimeOffsetProperties[node.Name];
            }

            return this.MakeFunctionCall(property, parameter);
        }

        private Expression BindTimeRelatedProperty(SingleValueFunctionCallNode node)
        {
            Expression[] arguments = this.BindArguments(node.Parameters);
            Contract.Assert(arguments.Length == 1 && (IsTimeRelated(arguments[0].Type)));

            // We should support DateTime & DateTimeOffset even though DateTime is not part of OData v4 Spec.
            Expression parameter = arguments[0];

            PropertyInfo property;
            if (IsTimeOfDay(parameter.Type))
            {
                Contract.Assert(ClrCanonicalFunctions.TimeOfDayProperties.ContainsKey(node.Name));
                property = ClrCanonicalFunctions.TimeOfDayProperties[node.Name];
            }
            else if (IsDateTime(parameter.Type))
            {
                Contract.Assert(ClrCanonicalFunctions.DateTimeProperties.ContainsKey(node.Name));
                property = ClrCanonicalFunctions.DateTimeProperties[node.Name];
            }
            else if (IsTimeSpan(parameter.Type))
            {
                Contract.Assert(ClrCanonicalFunctions.TimeSpanProperties.ContainsKey(node.Name));
                property = ClrCanonicalFunctions.TimeSpanProperties[node.Name];
            }
            else
            {
                Contract.Assert(ClrCanonicalFunctions.DateTimeOffsetProperties.ContainsKey(node.Name));
                property = ClrCanonicalFunctions.DateTimeOffsetProperties[node.Name];
            }

            return this.MakeFunctionCall(property, parameter);
        }

        private Expression BindConcat(SingleValueFunctionCallNode node)
        {
            Contract.Assert("concat" == node.Name);

            Expression[] arguments = this.BindArguments(node.Parameters);
            ValidateAllStringArguments(node.Name, arguments);

            Contract.Assert(arguments.Length == 2 && arguments[0].Type == typeof(string) && arguments[1].Type == typeof(string));

            return this.MakeFunctionCall(ClrCanonicalFunctions.Concat, arguments);
        }

        private Expression BindTrim(SingleValueFunctionCallNode node)
        {
            Contract.Assert("trim" == node.Name);

            Expression[] arguments = this.BindArguments(node.Parameters);
            ValidateAllStringArguments(node.Name, arguments);

            Contract.Assert(arguments.Length == 1 && arguments[0].Type == typeof(string));

            return this.MakeFunctionCall(ClrCanonicalFunctions.Trim, arguments);
        }

        private Expression BindToUpper(SingleValueFunctionCallNode node)
        {
            Contract.Assert("toupper" == node.Name);

            Expression[] arguments = this.BindArguments(node.Parameters);
            ValidateAllStringArguments(node.Name, arguments);

            Contract.Assert(arguments.Length == 1 && arguments[0].Type == typeof(string));

            return this.MakeFunctionCall(ClrCanonicalFunctions.ToUpper, arguments);
        }

        private Expression BindToLower(SingleValueFunctionCallNode node)
        {
            Contract.Assert("tolower" == node.Name);

            Expression[] arguments = this.BindArguments(node.Parameters);
            ValidateAllStringArguments(node.Name, arguments);

            Contract.Assert(arguments.Length == 1 && arguments[0].Type == typeof(string));

            return this.MakeFunctionCall(ClrCanonicalFunctions.ToLower, arguments);
        }

        private Expression BindIndexOf(SingleValueFunctionCallNode node)
        {
            Contract.Assert("indexof" == node.Name);

            Expression[] arguments = this.BindArguments(node.Parameters);
            ValidateAllStringArguments(node.Name, arguments);

            Contract.Assert(arguments.Length == 2 && arguments[0].Type == typeof(string) && arguments[1].Type == typeof(string));

            return this.MakeFunctionCall(ClrCanonicalFunctions.IndexOf, arguments);
        }

        private Expression BindSubstring(SingleValueFunctionCallNode node)
        {
            Contract.Assert("substring" == node.Name);

            Expression[] arguments = this.BindArguments(node.Parameters);
            if (arguments[0].Type != typeof(string))
            {
                throw new ODataException(Error.Format(SRResources.FunctionNotSupportedOnEnum, node.Name));
            }

            Expression functionCall;
            if (arguments.Length == 2)
            {
                Contract.Assert(IsInteger(arguments[1].Type));

                // When null propagation is allowed, we use a safe version of String.Substring(int).
                // But for providers that would not recognize custom expressions like this, we map
                // directly to String.Substring(int)
                if (this.QuerySettings.HandleNullPropagation == HandleNullPropagationOption.True)
                {
                    // Safe function is static and takes string "this" as first argument
                    functionCall = this.MakeFunctionCall(ClrCanonicalFunctions.SubstringStartNoThrow, arguments);
                }
                else
                {
                    functionCall = this.MakeFunctionCall(ClrCanonicalFunctions.SubstringStart, arguments);
                }
            }
            else
            {
                // arguments.Length == 3 implies String.Substring(int, int)
                Contract.Assert(arguments.Length == 3 && IsInteger(arguments[1].Type) && IsInteger(arguments[2].Type));

                // When null propagation is allowed, we use a safe version of String.Substring(int, int).
                // But for providers that would not recognize custom expressions like this, we map
                // directly to String.Substring(int, int)
                if (this.QuerySettings.HandleNullPropagation == HandleNullPropagationOption.True)
                {
                    // Safe function is static and takes string "this" as first argument
                    functionCall = this.MakeFunctionCall(ClrCanonicalFunctions.SubstringStartAndLengthNoThrow, arguments);
                }
                else
                {
                    functionCall = this.MakeFunctionCall(ClrCanonicalFunctions.SubstringStartAndLength, arguments);
                }
            }

            return functionCall;
        }

        private Expression BindLength(SingleValueFunctionCallNode node)
        {
            Contract.Assert("length" == node.Name);

            Expression[] arguments = this.BindArguments(node.Parameters);
            ValidateAllStringArguments(node.Name, arguments);

            Contract.Assert(arguments.Length == 1 && arguments[0].Type == typeof(string));

            return this.MakeFunctionCall(ClrCanonicalFunctions.Length, arguments);
        }

        private Expression BindContains(SingleValueFunctionCallNode node)
        {
            Contract.Assert("contains" == node.Name);

            Expression[] arguments = this.BindArguments(node.Parameters);
            ValidateAllStringArguments(node.Name, arguments);

            Contract.Assert(arguments.Length == 2 && arguments[0].Type == typeof(string) && arguments[1].Type == typeof(string));

            return this.MakeFunctionCall(ClrCanonicalFunctions.Contains, arguments[0], arguments[1]);
        }

        private Expression BindStartsWith(SingleValueFunctionCallNode node)
        {
            Contract.Assert("startswith" == node.Name);

            Expression[] arguments = this.BindArguments(node.Parameters);
            ValidateAllStringArguments(node.Name, arguments);

            Contract.Assert(arguments.Length == 2 && arguments[0].Type == typeof(string) && arguments[1].Type == typeof(string));

            return this.MakeFunctionCall(ClrCanonicalFunctions.StartsWith, arguments);
        }

        private Expression BindEndsWith(SingleValueFunctionCallNode node)
        {
            Contract.Assert("endswith" == node.Name);

            Expression[] arguments = this.BindArguments(node.Parameters);
            ValidateAllStringArguments(node.Name, arguments);

            Contract.Assert(arguments.Length == 2 && arguments[0].Type == typeof(string) && arguments[1].Type == typeof(string));

            return this.MakeFunctionCall(ClrCanonicalFunctions.EndsWith, arguments);
        }

        private Expression[] BindArguments(IEnumerable<QueryNode> nodes)
        {
            return nodes.OfType<SingleValueNode>().Select(n => this.Bind(n)).ToArray();
        }

        private static void ValidateAllStringArguments(string functionName, Expression[] arguments)
        {
            if (arguments.Any(arg => arg.Type != typeof(string)))
            {
                throw new ODataException(Error.Format(SRResources.FunctionNotSupportedOnEnum, functionName));
            }
        }

        /// <summary>
        /// Binds a <see cref="AllNode"/> to create a LINQ <see cref="Expression"/> that
        /// represents the semantics of the <see cref="AllNode"/>.
        /// </summary>
        /// <param name="allNode">The node to bind.</param>
        /// <returns>The LINQ <see cref="Expression"/> created.</returns>
        public virtual Expression BindAllNode(AllNode allNode)
        {
            ParameterExpression allIt = this.HandleLambdaParameters(allNode.RangeVariables);

            Expression source;
            Contract.Assert(allNode.Source != null);
            source = this.Bind(allNode.Source);

            Expression body = source;
            Contract.Assert(allNode.Body != null);

            body = this.Bind(allNode.Body);
            body = this.ApplyNullPropagationForFilterBody(body);
            body = Expression.Lambda(body, allIt);

            Expression all = All(source, body);

            this.ExitLamdbaScope();

            if (this.QuerySettings.HandleNullPropagation == HandleNullPropagationOption.True && IsNullable(source.Type))
            {
                // IFF(source == null) null; else Any(body);
                all = ToNullable(all);
                return Expression.Condition(
                    test: Expression.Equal(source, NullConstant),
                    ifTrue: Expression.Constant(null, all.Type),
                    ifFalse: all);
            }
            else
            {
                return all;
            }
        }

        /// <summary>
        /// Binds a <see cref="AnyNode"/> to create a LINQ <see cref="Expression"/> that
        /// represents the semantics of the <see cref="AnyNode"/>.
        /// </summary>
        /// <param name="anyNode">The node to bind.</param>
        /// <returns>The LINQ <see cref="Expression"/> created.</returns>
        public virtual Expression BindAnyNode(AnyNode anyNode)
        {
            ParameterExpression anyIt = this.HandleLambdaParameters(anyNode.RangeVariables);

            Expression source;
            Contract.Assert(anyNode.Source != null);
            source = this.Bind(anyNode.Source);

            Expression body = null;
            // uri parser places an Constant node with value true for empty any() body
            if (anyNode.Body != null && anyNode.Body.Kind != QueryNodeKind.Constant)
            {
                body = this.Bind(anyNode.Body);
                body = this.ApplyNullPropagationForFilterBody(body);
                body = Expression.Lambda(body, anyIt);
            }

            Expression any = Any(source, body);

            this.ExitLamdbaScope();

            if (this.QuerySettings.HandleNullPropagation == HandleNullPropagationOption.True && IsNullable(source.Type))
            {
                // IFF(source == null) null; else Any(body);
                any = ToNullable(any);
                return Expression.Condition(
                    test: Expression.Equal(source, NullConstant),
                    ifTrue: Expression.Constant(null, any.Type),
                    ifFalse: any);
            }
            else
            {
                return any;
            }
        }

        private Expression BindCustomMethodExpressionOrNull(SingleValueFunctionCallNode node)
        {
            Expression[] arguments = this.BindArguments(node.Parameters);
            IEnumerable<Type> methodArgumentsType = arguments.Select(argument => argument.Type);

            // Search for custom method info that are binded to the node name
            MethodInfo methodInfo;
            if (UriFunctionsBinder.TryGetMethodInfo(node.Name, methodArgumentsType, out methodInfo))
            {
                return this.MakeFunctionCall(methodInfo, arguments);
            }

            return null;
        }

        private ParameterExpression HandleLambdaParameters(IEnumerable<RangeVariable> rangeVariables)
        {
            ParameterExpression lambdaIt = null;

            this.EnterLambdaScope();

            Dictionary<string, ParameterExpression> newParameters = new Dictionary<string, ParameterExpression>();
            foreach (RangeVariable rangeVariable in rangeVariables)
            {
                ParameterExpression parameter;
                if (!this._lambdaParameters.TryGetValue(rangeVariable.Name, out parameter))
                {
                    // Work-around issue 481323 where UriParser yields a collection parameter type
                    // for primitive collections rather than the inner element type of the collection.
                    // Remove this block of code when 481323 is resolved.
                    IEdmTypeReference edmTypeReference = rangeVariable.TypeReference;
                    IEdmCollectionTypeReference collectionTypeReference = edmTypeReference as IEdmCollectionTypeReference;
                    if (collectionTypeReference != null)
                    {
                        IEdmCollectionType collectionType = collectionTypeReference.Definition as IEdmCollectionType;
                        if (collectionType != null)
                        {
                            edmTypeReference = collectionType.ElementType;
                        }
                    }

                    parameter = Expression.Parameter(EdmLibHelpers.GetClrType(edmTypeReference, this.Model), rangeVariable.Name);
                    Contract.Assert(lambdaIt == null, "There can be only one parameter in an Any/All lambda");
                    lambdaIt = parameter;
                }
                newParameters.Add(rangeVariable.Name, parameter);
            }

            this._lambdaParameters = newParameters;
            return lambdaIt;
        }

        private void EnterLambdaScope()
        {
            Contract.Assert(this._lambdaParameters != null);
            this._parametersStack.Push(this._lambdaParameters);
        }

        private void ExitLamdbaScope()
        {
            if (this._parametersStack.Count != 0)
            {
                this._lambdaParameters = this._parametersStack.Pop();
            }
            else
            {
                this._lambdaParameters = null;
            }
        }

        private static Expression Any(Expression source, Expression filter)
        {
            Contract.Assert(source != null);
            Type elementType;
            source.Type.IsCollection(out elementType);
            Contract.Assert(elementType != null);

            if (filter == null)
            {
                if (IsIQueryable(source.Type))
                {
                    return Expression.Call(null, ExpressionHelperMethods.QueryableEmptyAnyGeneric.MakeGenericMethod(elementType), source);
                }
                else
                {
                    return Expression.Call(null, ExpressionHelperMethods.EnumerableEmptyAnyGeneric.MakeGenericMethod(elementType), source);
                }
            }
            else
            {
                if (IsIQueryable(source.Type))
                {
                    return Expression.Call(null, ExpressionHelperMethods.QueryableNonEmptyAnyGeneric.MakeGenericMethod(elementType), source, filter);
                }
                else
                {
                    return Expression.Call(null, ExpressionHelperMethods.EnumerableNonEmptyAnyGeneric.MakeGenericMethod(elementType), source, filter);
                }
            }
        }

        private static Expression All(Expression source, Expression filter)
        {
            Contract.Assert(source != null);
            Contract.Assert(filter != null);

            Type elementType;
            source.Type.IsCollection(out elementType);
            Contract.Assert(elementType != null);

            if (IsIQueryable(source.Type))
            {
                return Expression.Call(null, ExpressionHelperMethods.QueryableAllGeneric.MakeGenericMethod(elementType), source, filter);
            }
            else
            {
                return Expression.Call(null, ExpressionHelperMethods.EnumerableAllGeneric.MakeGenericMethod(elementType), source, filter);
            }
        }
    }
}
