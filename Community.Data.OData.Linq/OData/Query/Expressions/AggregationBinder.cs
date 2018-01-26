// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Community.OData.Linq.OData.Query.Expressions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;

    using Community.OData.Linq.Common;
    using Community.OData.Linq.OData.Formatter;
    using Community.OData.Linq.Properties;

    using Microsoft.OData;
    using Microsoft.OData.Edm;
    using Microsoft.OData.UriParser;
    using Microsoft.OData.UriParser.Aggregation;

    internal class AggregationBinder : ExpressionBinderBase
    {
        private Type _elementType;
        private TransformationNode _transformation;

        private ParameterExpression _lambdaParameter;

        private IEnumerable<AggregateExpression> _aggregateExpressions;
        private IEnumerable<GroupByPropertyNode> _groupingProperties;

        private Type _groupByClrType;

        private bool _linqToObjectMode = false;

        internal AggregationBinder(ODataQuerySettings settings, IAssembliesResolver assembliesResolver, Type elementType,
            IEdmModel model, TransformationNode transformation)
            : base(model, assembliesResolver, settings)
        {
            Contract.Assert(elementType != null);
            Contract.Assert(transformation != null);

            this._elementType = elementType;
            this._transformation = transformation;

            this._lambdaParameter = Expression.Parameter(this._elementType, "$it");

            switch (transformation.Kind)
            {
                case TransformationNodeKind.Aggregate:
                    var aggregateClause = this._transformation as AggregateTransformationNode;
                    this._aggregateExpressions = this.FixCustomMethodReturnTypes(aggregateClause.Expressions);
                    this.ResultClrType = typeof(NoGroupByAggregationWrapper);
                    break;
                case TransformationNodeKind.GroupBy:
                    var groupByClause = this._transformation as GroupByTransformationNode;
                    this._groupingProperties = groupByClause.GroupingProperties;
                    if (groupByClause.ChildTransformations != null)
                    {
                        if (groupByClause.ChildTransformations.Kind == TransformationNodeKind.Aggregate)
                        {
                            var aggregationNode = (AggregateTransformationNode)groupByClause.ChildTransformations;
                            this._aggregateExpressions = this.FixCustomMethodReturnTypes(aggregationNode.Expressions);
                        }
                        else
                        {
                            throw new NotImplementedException();
                        }
                    }

                    this._groupByClrType = typeof(GroupByWrapper);
                    this.ResultClrType = typeof(AggregationWrapper);
                    break;
                default:
                    throw new NotSupportedException(String.Format(CultureInfo.InvariantCulture,
                        SRResources.NotSupportedTransformationKind, transformation.Kind));
            }

            this._groupByClrType = this._groupByClrType ?? typeof(NoGroupByWrapper);
        }

        private IEnumerable<AggregateExpression> FixCustomMethodReturnTypes(IEnumerable<AggregateExpression> aggregateExpressions)
        {
            return aggregateExpressions.Select(this.FixCustomMethodReturnType);
        }

        private AggregateExpression FixCustomMethodReturnType(AggregateExpression expression)
        {
            if (expression.Method != AggregationMethod.Custom)
            {
                return expression;
            }

            var customMethod = this.GetCustomMethod(expression);
            var typeReference = EdmLibHelpers.GetEdmPrimitiveTypeReferenceOrNull(customMethod.ReturnType);
            return new AggregateExpression(expression.Expression, expression.MethodDefinition, expression.Alias, typeReference);
        }

        private MethodInfo GetCustomMethod(AggregateExpression expression)
        {
            var propertyLambda = Expression.Lambda(this.BindAccessor(expression.Expression), this._lambdaParameter);
            Type inputType = propertyLambda.Body.Type;

            string methodToken = expression.MethodDefinition.MethodLabel;
            var customFunctionAnnotations = this.Model.GetAnnotationValue<CustomAggregateMethodAnnotation>(this.Model);

            MethodInfo customMethod;
            if (!customFunctionAnnotations.GetMethodInfo(methodToken, inputType, out customMethod))
            {
                throw new ODataException(
                    Error.Format(
                        SRResources.AggregationNotSupportedForType,
                        expression.Method,
                        expression.Expression,
                        inputType));
            }

            return customMethod;
        }

        /// <summary>
        /// Gets CLR type returned from the query.
        /// </summary>
        public Type ResultClrType
        {
            get; private set;
        }

        public IEdmTypeReference ResultType
        {
            get; private set;
        }

        public IQueryable Bind(IQueryable query)
        {
            Contract.Assert(query != null);

            this._linqToObjectMode = query.Provider.GetType().Namespace == HandleNullPropagationOptionHelper.Linq2ObjectsQueryProviderNamespace;
            this.BaseQuery = query;
            this.EnsureFlattenedPropertyContainer(this._lambdaParameter);

            // Answer is query.GroupBy($it => new DynamicType1() {...}).Select($it => new DynamicType2() {...})
            // We are doing Grouping even if only aggregate was specified to have a IQuaryable after aggregation
            IQueryable grouping = this.BindGroupBy(query);

            IQueryable result = this.BindSelect(grouping);

            return result;
        }

        private IQueryable BindSelect(IQueryable grouping)
        {
            // Should return following expression
            // .Select($it => New DynamicType2() 
            //                  {
            //                      GroupByContainer = $it.Key.GroupByContainer // If groupby section present
            //                      Container => new AggregationPropertyContainer() {
            //                          Name = "Alias1", 
            //                          Value = $it.AsQuaryable().Sum(i => i.AggregatableProperty), 
            //                          Next = new LastInChain() {
            //                              Name = "Alias2",
            //                              Value = $it.AsQuaryable().Sum(i => i.AggregatableProperty)
            //                          }
            //                      }
            //                  })
            var groupingType = typeof(IGrouping<,>).MakeGenericType(this._groupByClrType, this._elementType);
            ParameterExpression accum = Expression.Parameter(groupingType, "$it");

            List<MemberAssignment> wrapperTypeMemberAssignments = new List<MemberAssignment>();

            // Setting GroupByContainer property when previous step was grouping
            if (this._groupingProperties != null && this._groupingProperties.Any())
            {
                var wrapperProperty = this.ResultClrType.GetProperty("GroupByContainer");

                wrapperTypeMemberAssignments.Add(Expression.Bind(wrapperProperty, Expression.Property(Expression.Property(accum, "Key"), "GroupByContainer")));
            }

            // Setting Container property when we have aggregation clauses
            if (this._aggregateExpressions != null)
            {
                var properties = new List<NamedPropertyExpression>();
                foreach (var aggExpression in this._aggregateExpressions)
                {
                    properties.Add(new NamedPropertyExpression(Expression.Constant(aggExpression.Alias), this.CreateAggregationExpression(accum, aggExpression)));
                }

                var wrapperProperty = this.ResultClrType.GetProperty("Container");
                wrapperTypeMemberAssignments.Add(Expression.Bind(wrapperProperty, AggregationPropertyContainer.CreateNextNamedPropertyContainer(properties)));
            }

            var initilizedMember =
                Expression.MemberInit(Expression.New(this.ResultClrType), wrapperTypeMemberAssignments);
            var selectLambda = Expression.Lambda(initilizedMember, accum);

            var result = ExpressionHelpers.Select(grouping, selectLambda, groupingType);
            return result;
        }

        private List<MemberAssignment> CreateSelectMemberAssigments(Type type, MemberExpression propertyAccessor,
            IEnumerable<GroupByPropertyNode> properties)
        {
            var wrapperTypeMemberAssignments = new List<MemberAssignment>();
            if (this._groupingProperties != null)
            {
                foreach (var node in properties)
                {
                    var nodePropertyAccessor = Expression.Property(propertyAccessor, node.Name);
                    var member = type.GetMember(node.Name).Single();
                    if (node.Expression != null)
                    {
                        wrapperTypeMemberAssignments.Add(Expression.Bind(member, nodePropertyAccessor));
                    }
                    else
                    {
                        var memberType = (member as PropertyInfo).PropertyType;
                        var expr = Expression.MemberInit(Expression.New(memberType),
                            this.CreateSelectMemberAssigments(memberType, nodePropertyAccessor, node.ChildTransformations));
                        wrapperTypeMemberAssignments.Add(Expression.Bind(member, expr));
                    }
                }
            }

            return wrapperTypeMemberAssignments;
        }

        private Expression CreateAggregationExpression(ParameterExpression accum, AggregateExpression expression)
        {
            // I substitute the element type for all generic arguments.                                                
            var asQuerableMethod = ExpressionHelperMethods.QueryableAsQueryable.MakeGenericMethod(this._elementType);
            Expression asQuerableExpression = Expression.Call(null, asQuerableMethod, accum);

            // $count is a virtual property, so there's not a propertyLambda to create.
            if (expression.Method == AggregationMethod.VirtualPropertyCount)
            {
                var countMethod = ExpressionHelperMethods.QueryableCountGeneric.MakeGenericMethod(this._elementType);
                return this.WrapConvert(Expression.Call(null, countMethod, asQuerableExpression));
            }

            LambdaExpression propertyLambda = Expression.Lambda(this.BindAccessor(expression.Expression), this._lambdaParameter);

            Expression aggregationExpression;

            switch (expression.Method)
            {
                case AggregationMethod.Min:
                    {
                        var minMethod = ExpressionHelperMethods.QueryableMin.MakeGenericMethod(this._elementType,
                            propertyLambda.Body.Type);
                        aggregationExpression = Expression.Call(null, minMethod, asQuerableExpression, propertyLambda);
                    }
                    break;
                case AggregationMethod.Max:
                    {
                        var maxMethod = ExpressionHelperMethods.QueryableMax.MakeGenericMethod(this._elementType,
                            propertyLambda.Body.Type);
                        aggregationExpression = Expression.Call(null, maxMethod, asQuerableExpression, propertyLambda);
                    }
                    break;
                case AggregationMethod.Sum:
                    {
                        MethodInfo sumGenericMethod;
                        if (
                            !ExpressionHelperMethods.QueryableSumGenerics.TryGetValue(propertyLambda.Body.Type,
                                out sumGenericMethod))
                        {
                            throw new ODataException(Error.Format(SRResources.AggregationNotSupportedForType,
                                expression.Method, expression.Expression, propertyLambda.Body.Type));
                        }
                        var sumMethod = sumGenericMethod.MakeGenericMethod(this._elementType);
                        aggregationExpression = Expression.Call(null, sumMethod, asQuerableExpression, propertyLambda);
                    }
                    break;
                case AggregationMethod.Average:
                    {
                        MethodInfo averageGenericMethod;
                        if (
                            !ExpressionHelperMethods.QueryableAverageGenerics.TryGetValue(propertyLambda.Body.Type,
                                out averageGenericMethod))
                        {
                            throw new ODataException(Error.Format(SRResources.AggregationNotSupportedForType,
                                expression.Method, expression.Expression, propertyLambda.Body.Type));
                        }
                        var averageMethod = averageGenericMethod.MakeGenericMethod(this._elementType);
                        aggregationExpression = Expression.Call(null, averageMethod, asQuerableExpression, propertyLambda);
                    }
                    break;
                case AggregationMethod.CountDistinct:
                    {
                        // I select the specific field 
                        var selectMethod =
                            ExpressionHelperMethods.QueryableSelectGeneric.MakeGenericMethod(this._elementType,
                                propertyLambda.Body.Type);
                        Expression queryableSelectExpression = Expression.Call(null, selectMethod, asQuerableExpression,
                            propertyLambda);

                        // I run distinct over the set of items
                        var distinctMethod =
                            ExpressionHelperMethods.QueryableDistinct.MakeGenericMethod(propertyLambda.Body.Type);
                        Expression distinctExpression = Expression.Call(null, distinctMethod, queryableSelectExpression);

                        // I count the distinct items as the aggregation expression
                        var countMethod =
                            ExpressionHelperMethods.QueryableCountGeneric.MakeGenericMethod(propertyLambda.Body.Type);
                        aggregationExpression = Expression.Call(null, countMethod, distinctExpression);
                    }
                    break;
                case AggregationMethod.Custom:
                    {
                        MethodInfo customMethod = this.GetCustomMethod(expression);
                        var selectMethod =
                            ExpressionHelperMethods.QueryableSelectGeneric.MakeGenericMethod(this._elementType, propertyLambda.Body.Type);
                        var selectExpression = Expression.Call(null, selectMethod, asQuerableExpression, propertyLambda);
                        aggregationExpression = Expression.Call(null, customMethod, selectExpression);
                    }
                    break;
                default:
                    throw new ODataException(Error.Format(SRResources.AggregationMethodNotSupported, expression.Method));
            }

            return this.WrapConvert(aggregationExpression);
        }

        private Expression WrapConvert(Expression expression)
        {
            return this._linqToObjectMode
                ? Expression.Convert(expression, typeof(object))
                : expression;
        }

        private Expression BindAccessor(SingleValueNode node)
        {
            switch (node.Kind)
            {
                case QueryNodeKind.ResourceRangeVariableReference:
                    return this._lambdaParameter;
                case QueryNodeKind.SingleValuePropertyAccess:
                    var propAccessNode = node as SingleValuePropertyAccessNode;
                    return this.CreatePropertyAccessExpression(this.BindAccessor(propAccessNode.Source), propAccessNode.Property, this.GetFullPropertyPath(propAccessNode));
                case QueryNodeKind.SingleComplexNode:
                    var singleComplexNode = node as SingleComplexNode;
                    return this.CreatePropertyAccessExpression(this.BindAccessor(singleComplexNode.Source), singleComplexNode.Property, this.GetFullPropertyPath(singleComplexNode));
                case QueryNodeKind.SingleValueOpenPropertyAccess:
                    var openNode = node as SingleValueOpenPropertyAccessNode;
                    return this.GetFlattenedPropertyExpression(openNode.Name) ?? Expression.Property(this.BindAccessor(openNode.Source), openNode.Name);
                case QueryNodeKind.None:
                case QueryNodeKind.SingleNavigationNode:
                    var navNode = node as SingleNavigationNode;
                    return this.CreatePropertyAccessExpression(this.BindAccessor(navNode.Source), navNode.NavigationProperty);
                case QueryNodeKind.BinaryOperator:
                    var binaryNode = node as BinaryOperatorNode;
                    var leftExpression = this.BindAccessor(binaryNode.Left);
                    var rightExpression = this.BindAccessor(binaryNode.Right);
                    return this.CreateBinaryExpression(binaryNode.OperatorKind, leftExpression, rightExpression,
                        liftToNull: true);
                case QueryNodeKind.Convert:
                    var convertNode = node as ConvertNode;
                    return this.CreateConvertExpression(convertNode, this.BindAccessor(convertNode.Source));
                default:
                    throw Error.NotSupported(SRResources.QueryNodeBindingNotSupported, node.Kind,
                        typeof(AggregationBinder).Name);
            }
        }

        private Expression CreatePropertyAccessExpression(Expression source, IEdmProperty property, string propertyPath = null)
        {
            string propertyName = EdmLibHelpers.GetClrPropertyName(property, this.Model);
            propertyPath = propertyPath ?? propertyName;
            if (this.QuerySettings.HandleNullPropagation == HandleNullPropagationOption.True && IsNullable(source.Type) &&
                source != this._lambdaParameter)
            {
                Expression cleanSource = this.RemoveInnerNullPropagation(source);
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

        private IQueryable BindGroupBy(IQueryable query)
        {
            LambdaExpression groupLambda = null;
            if (this._groupingProperties != null && this._groupingProperties.Any())
            {
                // Generates expression
                // .GroupBy($it => new DynamicTypeWrapper()
                //                                      {
                //                                           GroupByContainer => new AggregationPropertyContainer() {
                //                                               Name = "Prop1", 
                //                                               Value = $it.Prop1,
                //                                               Next = new AggregationPropertyContainer() {
                //                                                   Name = "Prop2",
                //                                                   Value = $it.Prop2, // int
                //                                                   Next = new LastInChain() {
                //                                                       Name = "Prop3",
                //                                                       Value = $it.Prop3
                //                                                   }
                //                                               }
                //                                           }
                //                                      }) 
                List<NamedPropertyExpression> properties = this.CreateGroupByMemberAssignments(this._groupingProperties);

                var wrapperProperty = typeof(GroupByWrapper).GetProperty("GroupByContainer");
                List<MemberAssignment> wta = new List<MemberAssignment>();
                wta.Add(Expression.Bind(wrapperProperty, AggregationPropertyContainer.CreateNextNamedPropertyContainer(properties)));
                groupLambda = Expression.Lambda(Expression.MemberInit(Expression.New(typeof(GroupByWrapper)), wta), this._lambdaParameter);
            }
            else
            {
                // We do not have properties to aggregate
                // .GroupBy($it => new NoGroupByWrapper())
                groupLambda = Expression.Lambda(Expression.New(this._groupByClrType), this._lambdaParameter);
            }

            return ExpressionHelpers.GroupBy(query, groupLambda, this._elementType, this._groupByClrType);
        }

        private List<NamedPropertyExpression> CreateGroupByMemberAssignments(IEnumerable<GroupByPropertyNode> nodes)
        {
            var properties = new List<NamedPropertyExpression>();
            foreach (var grpProp in nodes)
            {
                var propertyName = grpProp.Name;
                if (grpProp.Expression != null)
                {
                    properties.Add(new NamedPropertyExpression(Expression.Constant(propertyName), this.WrapConvert(this.BindAccessor(grpProp.Expression))));
                }
                else
                {
                    var wrapperProperty = typeof(GroupByWrapper).GetProperty("GroupByContainer");
                    List<MemberAssignment> wta = new List<MemberAssignment>();
                    wta.Add(Expression.Bind(wrapperProperty, AggregationPropertyContainer.CreateNextNamedPropertyContainer(this.CreateGroupByMemberAssignments(grpProp.ChildTransformations))));
                    properties.Add(new NamedPropertyExpression(Expression.Constant(propertyName), Expression.MemberInit(Expression.New(typeof(GroupByWrapper)), wta)));
                }
            }

            return properties;
        }
    }
}
