// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Community.OData.Linq.Builder
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Reflection;

    using Community.OData.Linq.Common;
    using Community.OData.Linq.OData;
    using Community.OData.Linq.OData.Query;
    using Community.OData.Linq.Properties;

    using Microsoft.OData.Edm;
    using Microsoft.OData.Edm.Csdl;
    using Microsoft.OData.Edm.Validation;
    using Microsoft.OData.Edm.Vocabularies;

    internal static class EdmModelHelperMethods
    {
        public static IEdmModel BuildEdmModel(ODataModelBuilder builder)
        {
            if (builder == null)
            {
                throw Error.ArgumentNull("builder");
            }

            EdmModel model = new EdmModel();
            EdmEntityContainer container = new EdmEntityContainer(builder.Namespace, builder.ContainerName);

            // add types and sets, building an index on the way.
            IEnumerable<IEdmTypeConfiguration> configTypes = builder.StructuralTypes.Concat<IEdmTypeConfiguration>(builder.EnumTypes);
            EdmTypeMap edmMap = EdmTypeBuilder.GetTypesAndProperties(configTypes);
            Dictionary<Type, IEdmType> edmTypeMap = model.AddTypes(edmMap);

            // Add EntitySets and build the mapping between the EdmEntitySet and the NavigationSourceConfiguration
            NavigationSourceAndAnnotations[] entitySets = container.AddEntitySetAndAnnotations(builder, edmTypeMap);

            // Add Singletons and build the mapping between the EdmSingleton and the NavigationSourceConfiguration
            NavigationSourceAndAnnotations[] singletons = container.AddSingletonAndAnnotations(builder, edmTypeMap);

            // Merge EntitySets and Singletons together
            IEnumerable<NavigationSourceAndAnnotations> navigationSources = entitySets.Concat(singletons);

            // Build the navigation source map
            IDictionary<string, EdmNavigationSource> navigationSourceMap = model.GetNavigationSourceMap(edmMap, navigationSources);

            // Add the core vocabulary annotations
            model.AddCoreVocabularyAnnotations(navigationSources, edmMap);

            // TODO: support etags on contained nav props
            // Support for this in 5.x adds annotations to navigation properties. Ideally would add annotations to entity set/singleton for
            // containing type(s) with nav paths to the contained nav property.
            // model.AddNavPropAnnotations(builder, edmMap);  // implemented in EdmModelHelperMethods.cs of 5.x branch

            // Add the capabilities vocabulary annotations
            model.AddCapabilitiesVocabularyAnnotations(navigationSources, edmMap);

            // finish up
            model.AddElement(container);

            // build the map from IEdmEntityType to IEdmFunctionImport
            model.SetAnnotationValue<BindableOperationFinder>(model, new BindableOperationFinder(model));

            return model;
        }

        private static void AddTypes(this EdmModel model, Dictionary<Type, IEdmType> types)
        {
            Contract.Assert(model != null);
            Contract.Assert(types != null);

            foreach (IEdmType type in types.Values)
            {
                model.AddType(type);
            }
        }

        private static NavigationSourceAndAnnotations[] AddEntitySetAndAnnotations(this EdmEntityContainer container,
            ODataModelBuilder builder, Dictionary<Type, IEdmType> edmTypeMap)
        {
            IEnumerable<EntitySetConfiguration> configurations = builder.EntitySets;

            // build the entitysets
            IEnumerable<Tuple<EdmEntitySet, EntitySetConfiguration>> entitySets = AddEntitySets(configurations, container, edmTypeMap);

            // return the annotation array
            return entitySets.Select(e => new NavigationSourceAndAnnotations()
            {
                NavigationSource = e.Item1,
                Configuration = e.Item2,
                Url = new NavigationSourceUrlAnnotation { Url = e.Item2.GetUrl() }
            }).ToArray();
        }

        private static NavigationSourceAndAnnotations[] AddSingletonAndAnnotations(this EdmEntityContainer container,
            ODataModelBuilder builder, Dictionary<Type, IEdmType> edmTypeMap)
        {
            IEnumerable<SingletonConfiguration> configurations = builder.Singletons;

            // build the singletons
            IEnumerable<Tuple<EdmSingleton, SingletonConfiguration>> singletons = AddSingletons(configurations, container, edmTypeMap);

            // return the annotation array
            return singletons.Select(e => new NavigationSourceAndAnnotations()
            {
                NavigationSource = e.Item1,
                Configuration = e.Item2,
                Url = new NavigationSourceUrlAnnotation { Url = e.Item2.GetUrl() }
            }).ToArray();
        }

        private static IDictionary<string, EdmNavigationSource> GetNavigationSourceMap(this EdmModel model, EdmTypeMap edmMap,
            IEnumerable<NavigationSourceAndAnnotations> navigationSourceAndAnnotations)
        {
            // index the navigation source by name
            Dictionary<string, EdmNavigationSource> edmNavigationSourceMap = navigationSourceAndAnnotations.ToDictionary(e => e.NavigationSource.Name, e => e.NavigationSource);

            // apply the annotations
            foreach (NavigationSourceAndAnnotations navigationSourceAndAnnotation in navigationSourceAndAnnotations)
            {
                EdmNavigationSource navigationSource = navigationSourceAndAnnotation.NavigationSource;
                model.SetAnnotationValue(navigationSource, navigationSourceAndAnnotation.Url);

                AddNavigationBindings(edmMap, navigationSourceAndAnnotation.Configuration, navigationSource,
                    edmNavigationSourceMap);
            }

            return edmNavigationSourceMap;
        }

        private static void AddNavigationBindings(EdmTypeMap edmMap,
            NavigationSourceConfiguration navigationSourceConfiguration,
            EdmNavigationSource navigationSource,
            Dictionary<string, EdmNavigationSource> edmNavigationSourceMap)
        {
            foreach (var binding in navigationSourceConfiguration.Bindings)
            {
                NavigationPropertyConfiguration navigationProperty = binding.NavigationProperty;
                bool isContained = navigationProperty.ContainsTarget;

                IEdmType edmType = edmMap.EdmTypes[navigationProperty.DeclaringType.ClrType];
                IEdmStructuredType structuraType = edmType as IEdmStructuredType;
                IEdmNavigationProperty edmNavigationProperty = structuraType.NavigationProperties()
                    .Single(np => np.Name == navigationProperty.Name);

                string bindingPath = ConvertBindingPath(edmMap, binding);
                if (!isContained)
                {
                    // calculate the binding path
                    navigationSource.AddNavigationTarget(
                        edmNavigationProperty,
                        edmNavigationSourceMap[binding.TargetNavigationSource.Name],
                        new EdmPathExpression(bindingPath));
                }
            }
        }

        private static string ConvertBindingPath(EdmTypeMap edmMap, NavigationPropertyBindingConfiguration binding)
        {
            IList<string> bindings = new List<string>();
            foreach (var bindingInfo in binding.Path)
            {
                Type typeCast = bindingInfo as Type;
                PropertyInfo propertyInfo = bindingInfo as PropertyInfo;

                if (typeCast != null)
                {
                    IEdmType edmType = edmMap.EdmTypes[typeCast];
                    bindings.Add(edmType.FullTypeName());
                }
                else if (propertyInfo != null)
                {
                    bindings.Add(edmMap.EdmProperties[propertyInfo].Name);
                }
            }

            return String.Join("/", bindings);
        }

        private static void AddOperationParameters(EdmOperation operation, OperationConfiguration operationConfiguration, Dictionary<Type, IEdmType> edmTypeMap)
        {
            foreach (ParameterConfiguration parameter in operationConfiguration.Parameters)
            {
                bool isParameterOptional = parameter.OptionalParameter;
                IEdmTypeReference parameterTypeReference = GetEdmTypeReference(edmTypeMap, parameter.TypeConfiguration, nullable: isParameterOptional);
                IEdmOperationParameter operationParameter = new EdmOperationParameter(operation, parameter.Name, parameterTypeReference);
                operation.AddParameter(operationParameter);
            }
        }
        
        private static void ValidateOperationEntitySetPath(IEdmModel model, IEdmOperationImport operationImport, OperationConfiguration operationConfiguration)
        {
            IEdmOperationParameter operationParameter;
            Dictionary<IEdmNavigationProperty, IEdmPathExpression> relativeNavigations;
            IEnumerable<EdmError> edmErrors;
            if (operationConfiguration.EntitySetPath != null && !operationImport.TryGetRelativeEntitySetPath(model, out operationParameter, out relativeNavigations, out edmErrors))
            {
                throw Error.InvalidOperation(SRResources.OperationHasInvalidEntitySetPath, String.Join("/", operationConfiguration.EntitySetPath), operationConfiguration.FullyQualifiedName);
            }
        }
        
        private static Dictionary<Type, IEdmType> AddTypes(this EdmModel model, EdmTypeMap edmTypeMap)
        {
            // build types
            Dictionary<Type, IEdmType> edmTypes = edmTypeMap.EdmTypes;

            // Add an annotate types
            model.AddTypes(edmTypes);
            model.AddClrTypeAnnotations(edmTypes);

            // add annotation for properties
            Dictionary<PropertyInfo, IEdmProperty> edmProperties = edmTypeMap.EdmProperties;
            model.AddClrPropertyInfoAnnotations(edmProperties);
            model.AddPropertyRestrictionsAnnotations(edmTypeMap.EdmPropertiesRestrictions);
            model.AddPropertiesQuerySettings(edmTypeMap.EdmPropertiesQuerySettings);
            model.AddStructuredTypeQuerySettings(edmTypeMap.EdmStructuredTypeQuerySettings);
         
            // add dynamic dictionary property annotation for open types
            model.AddDynamicPropertyDictionaryAnnotations(edmTypeMap.OpenTypes);

            return edmTypes;
        }

        private static void AddType(this EdmModel model, IEdmType type)
        {
            if (type.TypeKind == EdmTypeKind.Complex)
            {
                model.AddElement(type as IEdmComplexType);
            }
            else if (type.TypeKind == EdmTypeKind.Entity)
            {
                model.AddElement(type as IEdmEntityType);
            }
            else if (type.TypeKind == EdmTypeKind.Enum)
            {
                model.AddElement(type as IEdmEnumType);
            }
            else
            {
                Contract.Assert(false, "Only ComplexTypes, EntityTypes and EnumTypes are supported.");
            }
        }

        private static EdmEntitySet AddEntitySet(this EdmEntityContainer container, EntitySetConfiguration entitySet, IDictionary<Type, IEdmType> edmTypeMap)
        {
            return container.AddEntitySet(entitySet.Name, (IEdmEntityType)edmTypeMap[entitySet.EntityType.ClrType]);
        }

        private static IEnumerable<Tuple<EdmEntitySet, EntitySetConfiguration>> AddEntitySets(IEnumerable<EntitySetConfiguration> entitySets, EdmEntityContainer container, Dictionary<Type, IEdmType> edmTypeMap)
        {
            return entitySets.Select(es => Tuple.Create(container.AddEntitySet(es, edmTypeMap), es));
        }

        private static EdmSingleton AddSingleton(this EdmEntityContainer container, SingletonConfiguration singletonType, IDictionary<Type, IEdmType> edmTypeMap)
        {
            return container.AddSingleton(singletonType.Name, (IEdmEntityType)edmTypeMap[singletonType.EntityType.ClrType]);
        }

        private static IEnumerable<Tuple<EdmSingleton, SingletonConfiguration>> AddSingletons(IEnumerable<SingletonConfiguration> singletons, EdmEntityContainer container, Dictionary<Type, IEdmType> edmTypeMap)
        {
            return singletons.Select(sg => Tuple.Create(container.AddSingleton(sg, edmTypeMap), sg));
        }

        private static void AddClrTypeAnnotations(this EdmModel model, Dictionary<Type, IEdmType> edmTypes)
        {
            foreach (KeyValuePair<Type, IEdmType> map in edmTypes)
            {
                // pre-populate the model with clr-type annotations so that we dont have to scan 
                // all loaded assemblies to find the clr type for an edm type that we build.
                IEdmType edmType = map.Value;
                Type clrType = map.Key;
                model.SetAnnotationValue<ClrTypeAnnotation>(edmType, new ClrTypeAnnotation(clrType));
            }
        }

        private static void AddClrPropertyInfoAnnotations(this EdmModel model, Dictionary<PropertyInfo, IEdmProperty> edmProperties)
        {
            foreach (KeyValuePair<PropertyInfo, IEdmProperty> edmPropertyMap in edmProperties)
            {
                IEdmProperty edmProperty = edmPropertyMap.Value;
                PropertyInfo clrProperty = edmPropertyMap.Key;
                if (edmProperty.Name != clrProperty.Name)
                {
                    model.SetAnnotationValue(edmProperty, new ClrPropertyInfoAnnotation(clrProperty));
                }
            }
        }

        private static void AddDynamicPropertyDictionaryAnnotations(this EdmModel model,
            Dictionary<IEdmStructuredType, PropertyInfo> openTypes)
        {
            foreach (KeyValuePair<IEdmStructuredType, PropertyInfo> openType in openTypes)
            {
                IEdmStructuredType edmStructuredType = openType.Key;
                PropertyInfo propertyInfo = openType.Value;
                model.SetAnnotationValue(edmStructuredType, new DynamicPropertyDictionaryAnnotation(propertyInfo));
            }
        }

        private static void AddPropertiesQuerySettings(this EdmModel model,
            Dictionary<IEdmProperty, ModelBoundQuerySettings> edmPropertiesQuerySettings)
        {
            foreach (KeyValuePair<IEdmProperty, ModelBoundQuerySettings> edmPropertiesQuerySetting in
                    edmPropertiesQuerySettings)
            {
                IEdmProperty edmProperty = edmPropertiesQuerySetting.Key;
                ModelBoundQuerySettings querySettings = edmPropertiesQuerySetting.Value;
                model.SetAnnotationValue(edmProperty, querySettings);
            }
        }

        private static void AddStructuredTypeQuerySettings(this EdmModel model,
            Dictionary<IEdmStructuredType, ModelBoundQuerySettings> edmStructuredTypeQuerySettings)
        {
            foreach (
                KeyValuePair<IEdmStructuredType, ModelBoundQuerySettings> edmStructuredTypeQuerySetting in
                    edmStructuredTypeQuerySettings)
            {
                IEdmStructuredType structuredType = edmStructuredTypeQuerySetting.Key;
                ModelBoundQuerySettings querySettings = edmStructuredTypeQuerySetting.Value;
                model.SetAnnotationValue(structuredType, querySettings);
            }
        }

        private static void AddPropertyRestrictionsAnnotations(this EdmModel model, Dictionary<IEdmProperty, QueryableRestrictions> edmPropertiesRestrictions)
        {
            foreach (KeyValuePair<IEdmProperty, QueryableRestrictions> edmPropertyRestriction in edmPropertiesRestrictions)
            {
                IEdmProperty edmProperty = edmPropertyRestriction.Key;
                QueryableRestrictions restrictions = edmPropertyRestriction.Value;
                model.SetAnnotationValue(edmProperty, new QueryableRestrictionsAnnotation(restrictions));
            }
        }

        private static void AddCoreVocabularyAnnotations(this EdmModel model, IEnumerable<NavigationSourceAndAnnotations> navigationSources, EdmTypeMap edmTypeMap)
        {
            Contract.Assert(model != null);
            Contract.Assert(edmTypeMap != null);

            if (navigationSources == null)
            {
                return;
            }

            foreach (NavigationSourceAndAnnotations source in navigationSources)
            {
                IEdmVocabularyAnnotatable navigationSource = source.NavigationSource as IEdmVocabularyAnnotatable;
                if (navigationSource == null)
                {
                    continue;
                }

                NavigationSourceConfiguration navigationSourceConfig = source.Configuration as NavigationSourceConfiguration;
                if (navigationSourceConfig == null)
                {
                    continue;
                }

                model.AddOptimisticConcurrencyAnnotation(navigationSource, navigationSourceConfig, edmTypeMap);
            }
        }

        [SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling",
            Justification = "Relies on many ODataLib classes.")]
        private static void AddOptimisticConcurrencyAnnotation(this EdmModel model, IEdmVocabularyAnnotatable target,
            NavigationSourceConfiguration navigationSourceConfiguration, EdmTypeMap edmTypeMap)
        {
            EntityTypeConfiguration entityTypeConfig = navigationSourceConfiguration.EntityType;

            IEnumerable<StructuralPropertyConfiguration> concurrencyPropertyies =
                entityTypeConfig.Properties.OfType<StructuralPropertyConfiguration>().Where(property => property.ConcurrencyToken);

            IList<IEdmStructuralProperty> edmProperties = new List<IEdmStructuralProperty>();

            foreach (StructuralPropertyConfiguration property in concurrencyPropertyies)
            {
                IEdmProperty value;
                if (edmTypeMap.EdmProperties.TryGetValue(property.PropertyInfo, out value))
                {
                    var item = value as IEdmStructuralProperty;
                    if (item != null)
                    {
                        edmProperties.Add(item);
                    }
                }
            }

            if (edmProperties.Any())
            {
                IEdmCollectionExpression collectionExpression = new EdmCollectionExpression(edmProperties.Select(p => new EdmPropertyPathExpression(p.Name)).ToArray());
                IEdmTerm term = Microsoft.OData.Edm.Vocabularies.V1.CoreVocabularyModel.ConcurrencyTerm;
                EdmVocabularyAnnotation annotation = new EdmVocabularyAnnotation(target, term, collectionExpression);
                annotation.SetSerializationLocation(model, EdmVocabularyAnnotationSerializationLocation.Inline);
                model.SetVocabularyAnnotation(annotation);
            }
        }

        private static void AddCapabilitiesVocabularyAnnotations(this EdmModel model, IEnumerable<NavigationSourceAndAnnotations> navigationSources, EdmTypeMap edmTypeMap)
        {
            Contract.Assert(model != null);
            Contract.Assert(edmTypeMap != null);

            if (navigationSources == null)
            {
                return;
            }

            foreach (NavigationSourceAndAnnotations source in navigationSources)
            {
                IEdmEntitySet entitySet = source.NavigationSource as IEdmEntitySet;
                if (entitySet == null)
                {
                    continue;
                }

                EntitySetConfiguration entitySetConfig = source.Configuration as EntitySetConfiguration;
                if (entitySetConfig == null)
                {
                    continue;
                }

                model.AddCountRestrictionsAnnotation(entitySet, entitySetConfig, edmTypeMap);
                model.AddNavigationRestrictionsAnnotation(entitySet, entitySetConfig, edmTypeMap);
                model.AddFilterRestrictionsAnnotation(entitySet, entitySetConfig, edmTypeMap);
                model.AddSortRestrictionsAnnotation(entitySet, entitySetConfig, edmTypeMap);
                model.AddExpandRestrictionsAnnotation(entitySet, entitySetConfig, edmTypeMap);
            }
        }

        private static void AddCountRestrictionsAnnotation(this EdmModel model, IEdmEntitySet target,
            EntitySetConfiguration entitySetConfiguration, EdmTypeMap edmTypeMap)
        {
            EntityTypeConfiguration entityTypeConfig = entitySetConfiguration.EntityType;

            IEnumerable<PropertyConfiguration> notCountableProperties = entityTypeConfig.Properties.Where(property => property.NotCountable);

            IList<IEdmProperty> nonCountableProperties = new List<IEdmProperty>();
            IList<IEdmNavigationProperty> nonCountableNavigationProperties = new List<IEdmNavigationProperty>();
            foreach (PropertyConfiguration property in notCountableProperties)
            {
                IEdmProperty value;
                if (edmTypeMap.EdmProperties.TryGetValue(property.PropertyInfo, out value))
                {
                    if (value != null && value.Type.TypeKind() == EdmTypeKind.Collection)
                    {
                        if (value.PropertyKind == EdmPropertyKind.Navigation)
                        {
                            nonCountableNavigationProperties.Add((IEdmNavigationProperty)value);
                        }
                        else
                        {
                            nonCountableProperties.Add(value);
                        }
                    }
                }
            }

            if (nonCountableProperties.Any() || nonCountableNavigationProperties.Any())
            {
                model.SetCountRestrictionsAnnotation(target, true, nonCountableProperties, nonCountableNavigationProperties);
            }
        }

        private static void AddNavigationRestrictionsAnnotation(this EdmModel model, IEdmEntitySet target,
            EntitySetConfiguration entitySetConfiguration, EdmTypeMap edmTypeMap)
        {
            EntityTypeConfiguration entityTypeConfig = entitySetConfiguration.EntityType;

            IEnumerable<PropertyConfiguration> notNavigableProperties = entityTypeConfig.Properties.Where(property => property.NotNavigable);

            IList<Tuple<IEdmNavigationProperty, CapabilitiesNavigationType>> properties =
                new List<Tuple<IEdmNavigationProperty, CapabilitiesNavigationType>>();
            foreach (PropertyConfiguration property in notNavigableProperties)
            {
                IEdmProperty value;
                if (edmTypeMap.EdmProperties.TryGetValue(property.PropertyInfo, out value))
                {
                    if (value != null && value.PropertyKind == EdmPropertyKind.Navigation)
                    {
                        properties.Add(new Tuple<IEdmNavigationProperty, CapabilitiesNavigationType>(
                            (IEdmNavigationProperty)value, CapabilitiesNavigationType.Recursive));
                    }
                }
            }

            if (properties.Any())
            {
                model.SetNavigationRestrictionsAnnotation(target, CapabilitiesNavigationType.Recursive, properties);
            }
        }

        private static void AddFilterRestrictionsAnnotation(this EdmModel model, IEdmEntitySet target,
            EntitySetConfiguration entitySetConfiguration, EdmTypeMap edmTypeMap)
        {
            EntityTypeConfiguration entityTypeConfig = entitySetConfiguration.EntityType;

            IEnumerable<PropertyConfiguration> notFilterProperties = entityTypeConfig.Properties.Where(property => property.NonFilterable);

            IList<IEdmProperty> properties = new List<IEdmProperty>();
            foreach (PropertyConfiguration property in notFilterProperties)
            {
                IEdmProperty value;
                if (edmTypeMap.EdmProperties.TryGetValue(property.PropertyInfo, out value))
                {
                    if (value != null)
                    {
                        properties.Add(value);
                    }
                }
            }

            if (properties.Any())
            {
                model.SetFilterRestrictionsAnnotation(target, true, true, null, properties);
            }
        }

        private static void AddSortRestrictionsAnnotation(this EdmModel model, IEdmEntitySet target,
            EntitySetConfiguration entitySetConfiguration, EdmTypeMap edmTypeMap)
        {
            EntityTypeConfiguration entityTypeConfig = entitySetConfiguration.EntityType;
            IEnumerable<PropertyConfiguration> nonSortableProperties = entityTypeConfig.Properties.Where(property => property.Unsortable);
            IList<IEdmProperty> properties = new List<IEdmProperty>();
            foreach (PropertyConfiguration property in nonSortableProperties)
            {
                IEdmProperty value;
                if (edmTypeMap.EdmProperties.TryGetValue(property.PropertyInfo, out value))
                {
                    if (value != null)
                    {
                        properties.Add(value);
                    }
                }
            }

            if (properties.Any())
            {
                model.SetSortRestrictionsAnnotation(target, true, null, null, properties);
            }
        }

        private static void AddExpandRestrictionsAnnotation(this EdmModel model, IEdmEntitySet target,
            EntitySetConfiguration entitySetConfiguration, EdmTypeMap edmTypeMap)
        {
            EntityTypeConfiguration entityTypeConfig = entitySetConfiguration.EntityType;
            IEnumerable<PropertyConfiguration> nonExpandableProperties = entityTypeConfig.Properties.Where(property => property.NotExpandable);     
            IList<IEdmNavigationProperty> properties = new List<IEdmNavigationProperty>();
            foreach (PropertyConfiguration property in nonExpandableProperties)
            {
                IEdmProperty value;
                if (edmTypeMap.EdmProperties.TryGetValue(property.PropertyInfo, out value))
                {
                    if (value != null && value.PropertyKind == EdmPropertyKind.Navigation)
                    {
                        properties.Add((IEdmNavigationProperty)value);
                    }
                }
            }

            if (properties.Any())
            {
                model.SetExpandRestrictionsAnnotation(target, true, properties);
            }
        }

        private static IEdmExpression GetEdmEntitySetExpression(IDictionary<string, EdmNavigationSource> navigationSources, OperationConfiguration operationConfiguration)
        {
            if (operationConfiguration.NavigationSource != null)
            {
                EdmNavigationSource navigationSource;
                if (navigationSources.TryGetValue(operationConfiguration.NavigationSource.Name, out navigationSource))
                {
                    EdmEntitySet entitySet = navigationSource as EdmEntitySet;
                    if (entitySet != null)
                    {
                        return new EdmPathExpression(entitySet.Name);
                    }
                }
                else
                {
                    throw Error.InvalidOperation(SRResources.EntitySetNotFoundForName, operationConfiguration.NavigationSource.Name);
                }
            }
            else if (operationConfiguration.EntitySetPath != null)
            {
                return new EdmPathExpression(operationConfiguration.EntitySetPath);
            }

            return null;
        }

        private static IEdmTypeReference GetEdmTypeReference(Dictionary<Type, IEdmType> availableTypes, IEdmTypeConfiguration configuration, bool nullable)
        {
            Contract.Assert(availableTypes != null);

            if (configuration == null)
            {
                return null;
            }

            EdmTypeKind kind = configuration.Kind;
            if (kind == EdmTypeKind.Collection)
            {
                CollectionTypeConfiguration collectionType = (CollectionTypeConfiguration)configuration;
                EdmCollectionType edmCollectionType =
                    new EdmCollectionType(GetEdmTypeReference(availableTypes, collectionType.ElementType, nullable));
                return new EdmCollectionTypeReference(edmCollectionType);
            }
            else
            {
                Type configurationClrType = TypeHelper.GetUnderlyingTypeOrSelf(configuration.ClrType);

                if (!configurationClrType.IsEnum)
                {
                    configurationClrType = configuration.ClrType;
                }

                IEdmType type;

                if (availableTypes.TryGetValue(configurationClrType, out type))
                {
                    if (kind == EdmTypeKind.Complex)
                    {
                        return new EdmComplexTypeReference((IEdmComplexType)type, nullable);
                    }
                    else if (kind == EdmTypeKind.Entity)
                    {
                        return new EdmEntityTypeReference((IEdmEntityType)type, nullable);
                    }
                    else if (kind == EdmTypeKind.Enum)
                    {
                        return new EdmEnumTypeReference((IEdmEnumType)type, nullable);
                    }
                    else
                    {
                        throw Error.InvalidOperation(SRResources.UnsupportedEdmTypeKind, kind.ToString());
                    }
                }
                else if (configuration.Kind == EdmTypeKind.Primitive)
                {
                    PrimitiveTypeConfiguration primitiveTypeConfiguration = configuration as PrimitiveTypeConfiguration;
                    return new EdmPrimitiveTypeReference(primitiveTypeConfiguration.EdmPrimitiveType, nullable);
                }
                else
                {
                    throw Error.InvalidOperation(SRResources.NoMatchingIEdmTypeFound, configuration.FullName);
                }
            }
        }

        internal static string GetNavigationSourceUrl(this IEdmModel model, IEdmNavigationSource navigationSource)
        {
            if (model == null)
            {
                throw Error.ArgumentNull("model");
            }

            if (navigationSource == null)
            {
                throw Error.ArgumentNull("navigationSource");
            }

            NavigationSourceUrlAnnotation annotation = model.GetAnnotationValue<NavigationSourceUrlAnnotation>(navigationSource);
            if (annotation == null)
            {
                return navigationSource.Name;
            }
            else
            {
                return annotation.Url;
            }
        }

        internal static IEnumerable<IEdmAction> GetAvailableActions(this IEdmModel model, IEdmEntityType entityType)
        {
            return model.GetAvailableOperations(entityType, false).OfType<IEdmAction>();
        }

        internal static IEnumerable<IEdmFunction> GetAvailableFunctions(this IEdmModel model, IEdmEntityType entityType)
        {
            return model.GetAvailableOperations(entityType, false).OfType<IEdmFunction>();
        }

        internal static IEnumerable<IEdmOperation> GetAvailableOperationsBoundToCollection(this IEdmModel model, IEdmEntityType entityType)
        {
            return model.GetAvailableOperations(entityType, true);
        }

        internal static IEnumerable<IEdmOperation> GetAvailableOperations(this IEdmModel model, IEdmEntityType entityType, bool boundToCollection = false)
        {
            if (model == null)
            {
                throw Error.ArgumentNull("model");
            }

            if (entityType == null)
            {
                throw Error.ArgumentNull("entityType");
            }

            BindableOperationFinder annotation = model.GetAnnotationValue<BindableOperationFinder>(model);
            if (annotation == null)
            {
                annotation = new BindableOperationFinder(model);
                model.SetAnnotationValue(model, annotation);
            }

            if (boundToCollection)
            {
                return annotation.FindOperationsBoundToCollection(entityType);
            }
            else
            {
                return annotation.FindOperations(entityType);
            }
        }
    }
}
