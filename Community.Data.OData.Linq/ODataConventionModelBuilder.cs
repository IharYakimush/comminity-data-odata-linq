﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Community.OData.Linq
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Reflection;

    using Community.OData.Linq.Builder;
    using Community.OData.Linq.Builder.Conventions;
    using Community.OData.Linq.Builder.Conventions.Attributes;
    using Community.OData.Linq.Common;
    using Community.OData.Linq.OData;
    using Community.OData.Linq.OData.Formatter;
    using Community.OData.Linq.Properties;

    using Microsoft.OData.Edm;

    /// <summary>
    /// <see cref="ODataConventionModelBuilder"/> is used to automatically map CLR classes to an EDM model based on a set of <see cref="IConvention"/>.
    /// </summary>
    [SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "Most of the referenced types are helper types needed for operation.")]
    public class ODataConventionModelBuilder : ODataModelBuilder
    {
        private static readonly List<IConvention> _conventions = new List<IConvention>
        {
            // type and property conventions (ordering is important here).
            new AbstractTypeDiscoveryConvention(),
            new DataContractAttributeEdmTypeConvention(),
            new NotMappedAttributeConvention(), // NotMappedAttributeConvention has to run before EntityKeyConvention
            new DataMemberAttributeEdmPropertyConvention(),
            new RequiredAttributeEdmPropertyConvention(),
            new ConcurrencyCheckAttributeEdmPropertyConvention(),
            new TimestampAttributeEdmPropertyConvention(),
            new ColumnAttributeEdmPropertyConvention(),
            new KeyAttributeEdmPropertyConvention(), // KeyAttributeEdmPropertyConvention has to run before EntityKeyConvention
            new EntityKeyConvention(),
            new ComplexTypeAttributeConvention(), // This has to run after Key conventions, basically overrules them if there is a ComplexTypeAttribute
            new IgnoreDataMemberAttributeEdmPropertyConvention(),
            new NotFilterableAttributeEdmPropertyConvention(),
            new NonFilterableAttributeEdmPropertyConvention(),
            new NotSortableAttributeEdmPropertyConvention(),
            new UnsortableAttributeEdmPropertyConvention(),
            new NotNavigableAttributeEdmPropertyConvention(),
            new NotExpandableAttributeEdmPropertyConvention(),
            new MediaTypeAttributeConvention(),
            new AutoExpandAttributeEdmPropertyConvention(),
            new AutoExpandAttributeEdmTypeConvention(),
            new MaxLengthAttributeEdmPropertyConvention(),

            new ExpandAttributeEdmPropertyConvention(),
            new ExpandAttributeEdmTypeConvention(),

            new OrderByAttributeEdmTypeConvention(),
            new FilterAttributeEdmTypeConvention(),
            new OrderByAttributeEdmPropertyConvention(),
            new FilterAttributeEdmPropertyConvention(),
            new SelectAttributeEdmTypeConvention(),
            new SelectAttributeEdmPropertyConvention(),

            // INavigationSourceConvention's

            new AssociationSetDiscoveryConvention(),

            // IEdmFunctionImportConventions's
        };

        // These hashset's keep track of edmtypes/navigation sources for which conventions
        // have been applied or being applied so that we don't run a convention twice on the
        // same type/set.
        private HashSet<StructuralTypeConfiguration> _mappedTypes;
        private HashSet<NavigationSourceConfiguration> _configuredNavigationSources;
        private HashSet<Type> _ignoredTypes;

        private IEnumerable<StructuralTypeConfiguration> _explicitlyAddedTypes;

        private bool _isModelBeingBuilt;
        private bool _isQueryCompositionMode;

        // build the mapping between type and its derived types to be used later.
        private Lazy<IDictionary<Type, List<Type>>> _allTypesWithDerivedTypeMapping;

        /// <summary>
        /// Initializes a new instance of the <see cref="ODataConventionModelBuilder"/> class.
        /// </summary>
        public ODataConventionModelBuilder()
        {
            this.Initialize(isQueryCompositionMode: false);
        }
        
        /// <summary>
        /// Gets or sets if model aliasing is enabled or not. The default value is true.
        /// </summary>
        public bool ModelAliasingEnabled { get; set; }

        /// <summary>
        /// This action is invoked after the <see cref="ODataConventionModelBuilder"/> has run all the conventions, but before the configuration is locked
        /// down and used to build the <see cref="IEdmModel"/>.
        /// </summary>
        /// <remarks>Use this action to modify the <see cref="ODataModelBuilder"/> configuration that has been inferred by convention.</remarks>
        public Action<ODataConventionModelBuilder> OnModelCreating { get; set; }

        internal void Initialize(bool isQueryCompositionMode)
        {
            this._isQueryCompositionMode = isQueryCompositionMode;
            this._configuredNavigationSources = new HashSet<NavigationSourceConfiguration>();
            this._mappedTypes = new HashSet<StructuralTypeConfiguration>();
            this._ignoredTypes = new HashSet<Type>();
            this.ModelAliasingEnabled = true;
            this._allTypesWithDerivedTypeMapping = new Lazy<IDictionary<Type, List<Type>>>(
                () => BuildDerivedTypesMapping(),
                isThreadSafe: false);
        }

        /// <summary>
        /// Excludes a type from the model. This is used to remove types from the model that were added by convention during initial model discovery.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>The same <c ref="ODataConventionModelBuilder"/> so that multiple calls can be chained.</returns>
        [SuppressMessage("Microsoft.Design", "CA1004: GenericMethodsShouldProvideTypeParameter", Justification = "easier to call the generic version than using typeof().")]
        public ODataConventionModelBuilder Ignore<T>()
        {
            this._ignoredTypes.Add(typeof(T));
            return this;
        }

        /// <summary>
        /// Excludes a type or types from the model. This is used to remove types from the model that were added by convention during initial model discovery.
        /// </summary>
        /// <param name="types">The types to be excluded from the model.</param>
        /// <returns>The same <c ref="ODataConventionModelBuilder"/> so that multiple calls can be chained.</returns>
        public ODataConventionModelBuilder Ignore(params Type[] types)
        {
            foreach (Type type in types)
            {
                this._ignoredTypes.Add(type);
            }

            return this;
        }

        /// <inheritdoc />
        public override EntityTypeConfiguration AddEntityType(Type type)
        {
            EntityTypeConfiguration entityTypeConfiguration = base.AddEntityType(type);
            if (this._isModelBeingBuilt)
            {
                this.MapType(entityTypeConfiguration);
            }

            return entityTypeConfiguration;
        }

        /// <inheritdoc />
        public override ComplexTypeConfiguration AddComplexType(Type type)
        {
            ComplexTypeConfiguration complexTypeConfiguration = base.AddComplexType(type);
            if (this._isModelBeingBuilt)
            {
                this.MapType(complexTypeConfiguration);
            }

            return complexTypeConfiguration;
        }

        /// <inheritdoc />
        public override EntitySetConfiguration AddEntitySet(string name, EntityTypeConfiguration entityType)
        {
            EntitySetConfiguration entitySetConfiguration = base.AddEntitySet(name, entityType);
            if (this._isModelBeingBuilt)
            {
                this.ApplyNavigationSourceConventions(entitySetConfiguration);
            }

            return entitySetConfiguration;
        }

        /// <inheritdoc />
        public override SingletonConfiguration AddSingleton(string name, EntityTypeConfiguration entityType)
        {
            SingletonConfiguration singletonConfiguration = base.AddSingleton(name, entityType);
            if (this._isModelBeingBuilt)
            {
                this.ApplyNavigationSourceConventions(singletonConfiguration);
            }

            return singletonConfiguration;
        }

        /// <inheritdoc />
        public override EnumTypeConfiguration AddEnumType(Type type)
        {
            if (type == null)
            {
                throw Error.ArgumentNull("type");
            }

            if (!type.IsEnum)
            {
                throw Error.Argument("type", SRResources.TypeCannotBeEnum, type.FullName);
            }

            EnumTypeConfiguration enumTypeConfiguration = this.EnumTypes.SingleOrDefault(e => e.ClrType == type);

            if (enumTypeConfiguration == null)
            {
                enumTypeConfiguration = base.AddEnumType(type);

                foreach (object member in Enum.GetValues(type))
                {
                    bool addedExplicitly = enumTypeConfiguration.Members.Any(m => m.Name.Equals(member.ToString()));
                    EnumMemberConfiguration enumMemberConfiguration = enumTypeConfiguration.AddMember((Enum)member);
                    enumMemberConfiguration.AddedExplicitly = addedExplicitly;
                }
                this.ApplyEnumTypeConventions(enumTypeConfiguration);
            }

            return enumTypeConfiguration;
        }

        /// <inheritdoc />
        public override IEdmModel GetEdmModel()
        {
            if (this._isModelBeingBuilt)
            {
                throw Error.NotSupported(SRResources.GetEdmModelCalledMoreThanOnce);
            }

            // before we begin, get the set of types the user had added explicitly.
            this._explicitlyAddedTypes = new List<StructuralTypeConfiguration>(this.StructuralTypes);

            this._isModelBeingBuilt = true;

            this.MapTypes();

            this.DiscoverInheritanceRelationships();

            // Don't RediscoverComplexTypes() and treat everything as an entity type if building a model for EnableQueryAttribute.
            if (!this._isQueryCompositionMode)
            {
                this.RediscoverComplexTypes();
            }

            // prune unreachable types
            this.PruneUnreachableTypes();

            // Apply navigation source conventions.
            IEnumerable<NavigationSourceConfiguration> explictlyConfiguredNavigationSource =
                new List<NavigationSourceConfiguration>(this.NavigationSources);
            foreach (NavigationSourceConfiguration navigationSource in explictlyConfiguredNavigationSource)
            {
                this.ApplyNavigationSourceConventions(navigationSource);
            }

            foreach (OperationConfiguration operation in this.Operations)
            {
                this.ApplyOperationConventions(operation);
            }

            if (this.OnModelCreating != null)
            {
                this.OnModelCreating(this);
            }

            return base.GetEdmModel();
        }

        internal bool IsIgnoredType(Type type)
        {
            Contract.Requires(type != null);

            return this._ignoredTypes.Contains(type);
        }

        // patch up the base type for all types that don't have any yet.
        internal void DiscoverInheritanceRelationships()
        {
            Dictionary<Type, EntityTypeConfiguration> entityMap = this.StructuralTypes.OfType<EntityTypeConfiguration>().ToDictionary(e => e.ClrType);

            foreach (EntityTypeConfiguration entity in this.StructuralTypes.OfType<EntityTypeConfiguration>().Where(e => !e.BaseTypeConfigured))
            {
                Type baseClrType = entity.ClrType.BaseType;
                while (baseClrType != null)
                {
                    // see if we there is an entity that we know mapping to this clr types base type.
                    EntityTypeConfiguration baseEntityType;
                    if (entityMap.TryGetValue(baseClrType, out baseEntityType))
                    {
                        this.RemoveBaseTypeProperties(entity, baseEntityType);

                        // disable derived type key check if we are building a model for query composition.
                        if (this._isQueryCompositionMode)
                        {
                            // modifying the collection in the iterator, hence the ToArray().
                            foreach (PrimitivePropertyConfiguration keyProperty in entity.Keys.ToArray())
                            {
                                entity.RemoveKey(keyProperty);
                            }

                            foreach (EnumPropertyConfiguration enumKeyProperty in entity.EnumKeys.ToArray())
                            {
                                entity.RemoveKey(enumKeyProperty);
                            }
                        }

                        entity.DerivesFrom(baseEntityType);
                        break;
                    }

                    baseClrType = baseClrType.BaseType;
                }
            }

            Dictionary<Type, ComplexTypeConfiguration> complexMap =
                this.StructuralTypes.OfType<ComplexTypeConfiguration>().ToDictionary(e => e.ClrType);
            foreach (ComplexTypeConfiguration complex in
                this.StructuralTypes.OfType<ComplexTypeConfiguration>().Where(e => !e.BaseTypeConfigured))
            {
                Type baseClrType = complex.ClrType.BaseType;
                while (baseClrType != null)
                {
                    ComplexTypeConfiguration baseComplexType;
                    if (complexMap.TryGetValue(baseClrType, out baseComplexType))
                    {
                        this.RemoveBaseTypeProperties(complex, baseComplexType);
                        complex.DerivesFrom(baseComplexType);
                        break;
                    }

                    baseClrType = baseClrType.BaseType;
                }
            }
        }

        // remove the base type properties from the derived types.
        internal void RemoveBaseTypeProperties(StructuralTypeConfiguration derivedStructrualType,
            StructuralTypeConfiguration baseStructuralType)
        {
            IEnumerable<StructuralTypeConfiguration> typesToLift = new[] { derivedStructrualType }
                .Concat(this.DerivedTypes(derivedStructrualType));

            foreach (PropertyConfiguration property in baseStructuralType.Properties
                .Concat(baseStructuralType.DerivedProperties()))
            {
                foreach (StructuralTypeConfiguration structuralType in typesToLift)
                {
                    PropertyConfiguration derivedPropertyToRemove = structuralType.Properties.SingleOrDefault(
                        p => p.PropertyInfo.Name == property.PropertyInfo.Name);
                    if (derivedPropertyToRemove != null)
                    {
                        structuralType.RemoveProperty(derivedPropertyToRemove.PropertyInfo);
                    }
                }
            }

            foreach (PropertyInfo ignoredProperty in baseStructuralType.IgnoredProperties())
            {
                foreach (StructuralTypeConfiguration structuralType in typesToLift)
                {
                    PropertyConfiguration derivedPropertyToRemove = structuralType.Properties.SingleOrDefault(
                        p => p.PropertyInfo.Name == ignoredProperty.Name);
                    if (derivedPropertyToRemove != null)
                    {
                        structuralType.RemoveProperty(derivedPropertyToRemove.PropertyInfo);
                    }
                }
            }
        }

        private void RediscoverComplexTypes()
        {
            Contract.Assert(this._explicitlyAddedTypes != null);

            EntityTypeConfiguration[] misconfiguredEntityTypes = this.StructuralTypes
                                                                            .Except(this._explicitlyAddedTypes)
                                                                            .OfType<EntityTypeConfiguration>()
                                                                            .Where(entity => !entity.Keys().Any())
                                                                            .ToArray();

            this.ReconfigureEntityTypesAsComplexType(misconfiguredEntityTypes);

            this.DiscoverInheritanceRelationships();
        }

        private void ReconfigureEntityTypesAsComplexType(EntityTypeConfiguration[] misconfiguredEntityTypes)
        {
            IList<EntityTypeConfiguration> actualEntityTypes =
                this.StructuralTypes.OfType<EntityTypeConfiguration>()
                    .Where(entity => entity.Keys().Any())
                    .Concat(this._explicitlyAddedTypes.OfType<EntityTypeConfiguration>())
                    .Except(misconfiguredEntityTypes)
                    .ToList();

            HashSet<EntityTypeConfiguration> visitedEntityType = new HashSet<EntityTypeConfiguration>();
            foreach (EntityTypeConfiguration misconfiguredEntityType in misconfiguredEntityTypes)
            {
                if (visitedEntityType.Contains(misconfiguredEntityType))
                {
                    continue;
                }

                // If one of the base types is already configured as entity type, we should keep this type as entity type.
                IEnumerable<EntityTypeConfiguration> basedTypes = misconfiguredEntityType
                    .BaseTypes().OfType<EntityTypeConfiguration>();
                if (actualEntityTypes.Any(e => basedTypes.Any(a => a.ClrType == e.ClrType)))
                {
                    visitedEntityType.Add(misconfiguredEntityType);
                    continue;
                }

                // Make sure to remove current type and all the derived types
                IList<EntityTypeConfiguration> thisAndDerivedTypes = this.DerivedTypes(misconfiguredEntityType)
                    .Concat(new[] { misconfiguredEntityType }).OfType<EntityTypeConfiguration>().ToList();
                foreach (EntityTypeConfiguration subEnityType in thisAndDerivedTypes)
                {
                    if (actualEntityTypes.Any(e => e.ClrType == subEnityType.ClrType))
                    {
                        throw Error.InvalidOperation(SRResources.CannotReconfigEntityTypeAsComplexType,
                            misconfiguredEntityType.ClrType.FullName, subEnityType.ClrType.FullName);
                    }

                    this.RemoveStructuralType(subEnityType.ClrType);
                }

                // this is a wrongly inferred type. so just ignore any pending configuration from it.
                this.AddComplexType(misconfiguredEntityType.ClrType);

                foreach (EntityTypeConfiguration subEnityType in thisAndDerivedTypes)
                {
                    visitedEntityType.Add(subEnityType);

                    // go through all structural types to remove all properties defined by this mis-configed type.
                    IList<StructuralTypeConfiguration> allTypes = this.StructuralTypes.ToList();
                    foreach (StructuralTypeConfiguration structuralToBePatched in allTypes)
                    {
                        NavigationPropertyConfiguration[] propertiesToBeRemoved = structuralToBePatched
                            .NavigationProperties
                            .Where(navigationProperty => navigationProperty.RelatedClrType == subEnityType.ClrType)
                            .ToArray();

                        foreach (NavigationPropertyConfiguration propertyToBeRemoved in propertiesToBeRemoved)
                        {
                            string propertyNameAlias = propertyToBeRemoved.Name;
                            PropertyConfiguration propertyConfiguration;

                            structuralToBePatched.RemoveProperty(propertyToBeRemoved.PropertyInfo);

                            if (propertyToBeRemoved.Multiplicity == EdmMultiplicity.Many)
                            {
                                propertyConfiguration =
                                    structuralToBePatched.AddCollectionProperty(propertyToBeRemoved.PropertyInfo);
                            }
                            else
                            {
                                propertyConfiguration =
                                    structuralToBePatched.AddComplexProperty(propertyToBeRemoved.PropertyInfo);
                            }

                            Contract.Assert(propertyToBeRemoved.AddedExplicitly == false);

                            // The newly added property must be marked as added implicitly. This can make sure the property
                            // conventions can be re-applied to the new property.
                            propertyConfiguration.AddedExplicitly = false;

                            this.ReapplyPropertyConvention(propertyConfiguration, structuralToBePatched);

                            propertyConfiguration.Name = propertyNameAlias;
                        }
                    }
                }
            }
        }

        private void MapTypes()
        {
            foreach (StructuralTypeConfiguration edmType in this._explicitlyAddedTypes)
            {
                this.MapType(edmType);
            }

            // Apply foreign key conventions after the type mapping, because foreign key conventions depend on
            // entity key setting to be finished.
            this.ApplyForeignKeyConventions();
        }

        private void ApplyForeignKeyConventions()
        {
            ForeignKeyAttributeConvention foreignKeyAttributeConvention = new ForeignKeyAttributeConvention();
            ForeignKeyDiscoveryConvention foreignKeyDiscoveryConvention = new ForeignKeyDiscoveryConvention();
            ActionOnDeleteAttributeConvention actionOnDeleteConvention = new ActionOnDeleteAttributeConvention();
            foreach (EntityTypeConfiguration edmType in this.StructuralTypes.OfType<EntityTypeConfiguration>())
            {
                foreach (PropertyConfiguration property in edmType.Properties)
                {
                    // ForeignKeyDiscoveryConvention has to run after ForeignKeyAttributeConvention
                    foreignKeyAttributeConvention.Apply(property, edmType, this);
                    foreignKeyDiscoveryConvention.Apply(property, edmType, this);

                    actionOnDeleteConvention.Apply(property, edmType, this);
                }
            }
        }

        private void MapType(StructuralTypeConfiguration edmType)
        {
            if (!this._mappedTypes.Contains(edmType))
            {
                this._mappedTypes.Add(edmType);

                this.MapStructuralType(edmType);

                this.ApplyTypeAndPropertyConventions(edmType);
            }
        }

        private void MapStructuralType(StructuralTypeConfiguration structuralType)
        {
            IEnumerable<PropertyInfo> properties = ConventionsHelpers.GetProperties(structuralType, includeReadOnly: this._isQueryCompositionMode);
            foreach (PropertyInfo property in properties)
            {
                bool isCollection;
                IEdmTypeConfiguration mappedType;

                PropertyKind propertyKind = this.GetPropertyType(property, out isCollection, out mappedType);

                if (propertyKind == PropertyKind.Primitive || propertyKind == PropertyKind.Complex || propertyKind == PropertyKind.Enum)
                {
                    this.MapStructuralProperty(structuralType, property, propertyKind, isCollection);
                }
                else if (propertyKind == PropertyKind.Dynamic)
                {
                    structuralType.AddDynamicPropertyDictionary(property);
                }
                else
                {
                    // don't add this property if the user has already added it.
                    if (structuralType.NavigationProperties.All(p => p.Name != property.Name))
                    {
                        NavigationPropertyConfiguration addedNavigationProperty;
                        if (!isCollection)
                        {
                            addedNavigationProperty = structuralType.AddNavigationProperty(property, EdmMultiplicity.ZeroOrOne);
                        }
                        else
                        {
                            addedNavigationProperty = structuralType.AddNavigationProperty(property, EdmMultiplicity.Many);
                        }

                        ContainedAttribute containedAttribute = property.GetCustomAttribute<ContainedAttribute>();
                        if (containedAttribute != null)
                        {
                            addedNavigationProperty.Contained();
                        }

                        addedNavigationProperty.AddedExplicitly = false;
                    }
                }
            }

            this.MapDerivedTypes(structuralType);
        }

        internal void MapDerivedTypes(StructuralTypeConfiguration structuralType)
        {
            HashSet<Type> visitedTypes = new HashSet<Type>();

            Queue<StructuralTypeConfiguration> typeToBeVisited = new Queue<StructuralTypeConfiguration>();
            typeToBeVisited.Enqueue(structuralType);

            // populate all the derived complex types
            while (typeToBeVisited.Count != 0)
            {
                StructuralTypeConfiguration baseType = typeToBeVisited.Dequeue();
                visitedTypes.Add(baseType.ClrType);

                List<Type> derivedTypes;
                if (this._allTypesWithDerivedTypeMapping.Value.TryGetValue(baseType.ClrType, out derivedTypes))
                {
                    foreach (Type derivedType in derivedTypes)
                    {
                        if (!visitedTypes.Contains(derivedType) && !this.IsIgnoredType(derivedType))
                        {
                            StructuralTypeConfiguration derivedStructuralType;
                            if (baseType.Kind == EdmTypeKind.Entity)
                            {
                                derivedStructuralType = this.AddEntityType(derivedType);
                            }
                            else
                            {
                                derivedStructuralType = this.AddComplexType(derivedType);
                            }

                            typeToBeVisited.Enqueue(derivedStructuralType);
                        }
                    }
                }
            }
        }

        private void MapStructuralProperty(StructuralTypeConfiguration type, PropertyInfo property, PropertyKind propertyKind, bool isCollection)
        {
            Contract.Assert(type != null);
            Contract.Assert(property != null);
            Contract.Assert(propertyKind == PropertyKind.Complex || propertyKind == PropertyKind.Primitive || propertyKind == PropertyKind.Enum);

            bool addedExplicitly = type.Properties.Any(p => p.PropertyInfo.Name == property.Name);

            PropertyConfiguration addedEdmProperty;
            if (!isCollection)
            {
                if (propertyKind == PropertyKind.Primitive)
                {
                    addedEdmProperty = type.AddProperty(property);
                }
                else if (propertyKind == PropertyKind.Enum)
                {
                    this.AddEnumType(TypeHelper.GetUnderlyingTypeOrSelf(property.PropertyType));
                    addedEdmProperty = type.AddEnumProperty(property);
                }
                else
                {
                    addedEdmProperty = type.AddComplexProperty(property);
                }
            }
            else
            {
                if (this._isQueryCompositionMode)
                {
                    Contract.Assert(propertyKind != PropertyKind.Complex, "we don't create complex types in query composition mode.");
                }

                if (property.PropertyType.IsGenericType)
                {
                    Type elementType = property.PropertyType.GetGenericArguments().First();
                    Type elementUnderlyingTypeOrSelf = TypeHelper.GetUnderlyingTypeOrSelf(elementType);

                    if (elementUnderlyingTypeOrSelf.IsEnum)
                    {
                        this.AddEnumType(elementUnderlyingTypeOrSelf);
                    }
                }
                else
                {
                    Type elementType;
                    if (property.PropertyType.IsCollection(out elementType))
                    {
                        Type elementUnderlyingTypeOrSelf = TypeHelper.GetUnderlyingTypeOrSelf(elementType);
                        if (elementUnderlyingTypeOrSelf.IsEnum)
                        {
                            this.AddEnumType(elementUnderlyingTypeOrSelf);
                        }
                    }
                }

                addedEdmProperty = type.AddCollectionProperty(property);
            }

            addedEdmProperty.AddedExplicitly = addedExplicitly;
        }

        // figures out the type of the property (primitive, complex, navigation) and the corresponding edm type if we have seen this type
        // earlier or the user told us about it.
        private PropertyKind GetPropertyType(PropertyInfo property, out bool isCollection, out IEdmTypeConfiguration mappedType)
        {
            Contract.Assert(property != null);

            // IDictionary<string, object> is used as a container to save/retrieve dynamic properties for an open type.
            // It is different from other collections (for example, IEnumerable<T> or IDictionary<string, int>)
            // which are used as navigation properties.
            if (typeof(IDictionary<string, object>).IsAssignableFrom(property.PropertyType))
            {
                mappedType = null;
                isCollection = false;
                return PropertyKind.Dynamic;
            }

            PropertyKind propertyKind;
            if (this.TryGetPropertyTypeKind(property.PropertyType, out mappedType, out propertyKind))
            {
                isCollection = false;
                return propertyKind;
            }

            Type elementType;
            if (property.PropertyType.IsCollection(out elementType))
            {
                isCollection = true;
                if (this.TryGetPropertyTypeKind(elementType, out mappedType, out propertyKind))
                {
                    return propertyKind;
                }

                // if we know nothing about this type we assume it to be collection of entities
                // and patch up later
                return PropertyKind.Navigation;
            }

            // if we know nothing about this type we assume it to be an entity
            // and patch up later
            isCollection = false;
            return PropertyKind.Navigation;
        }

        private bool TryGetPropertyTypeKind(Type propertyType, out IEdmTypeConfiguration mappedType, out PropertyKind propertyKind)
        {
            Contract.Assert(propertyType != null);

            if (EdmLibHelpers.GetEdmPrimitiveTypeOrNull(propertyType) != null)
            {
                mappedType = null;
                propertyKind = PropertyKind.Primitive;
                return true;
            }

            mappedType = this.GetStructuralTypeOrNull(propertyType);
            if (mappedType != null)
            {
                if (mappedType is ComplexTypeConfiguration)
                {
                    propertyKind = PropertyKind.Complex;
                }
                else if (mappedType is EnumTypeConfiguration)
                {
                    propertyKind = PropertyKind.Enum;
                }
                else
                {
                    propertyKind = PropertyKind.Navigation;
                }

                return true;
            }

            // If one of the base types is configured as complex type, the type of this property
            // should be configured as complex type too.
            Type baseType = propertyType.BaseType;
            while (baseType != null && baseType != typeof(object))
            {
                IEdmTypeConfiguration baseMappedType = this.GetStructuralTypeOrNull(baseType);
                if (baseMappedType != null)
                {
                    if (baseMappedType is ComplexTypeConfiguration)
                    {
                        propertyKind = PropertyKind.Complex;
                        return true;
                    }
                }

                baseType = baseType.BaseType;
            }

            // refer the Edm type from the derived types
            PropertyKind referedPropertyKind = PropertyKind.Navigation;
            if (this.InferEdmTypeFromDerivedTypes(propertyType, ref referedPropertyKind))
            {
                if (referedPropertyKind == PropertyKind.Complex)
                {
                    this.ReconfigInferedEntityTypeAsComplexType(propertyType);
                }

                propertyKind = referedPropertyKind;
                return true;
            }

            if (TypeHelper.IsEnum(propertyType))
            {
                propertyKind = PropertyKind.Enum;
                return true;
            }

            propertyKind = PropertyKind.Navigation;
            return false;
        }

        internal void ReconfigInferedEntityTypeAsComplexType(Type propertyType)
        {
            HashSet<Type> visitedTypes = new HashSet<Type>();

            Queue<Type> typeToBeVisited = new Queue<Type>();
            typeToBeVisited.Enqueue(propertyType);

            IList<EntityTypeConfiguration> foundMappedTypes = new List<EntityTypeConfiguration>();
            while (typeToBeVisited.Count != 0)
            {
                Type currentType = typeToBeVisited.Dequeue();
                visitedTypes.Add(currentType);

                List<Type> derivedTypes;
                if (this._allTypesWithDerivedTypeMapping.Value.TryGetValue(currentType, out derivedTypes))
                {
                    foreach (Type derivedType in derivedTypes)
                    {
                        if (!visitedTypes.Contains(derivedType))
                        {
                            StructuralTypeConfiguration structuralType = this.StructuralTypes.Except(this._explicitlyAddedTypes)
                                .FirstOrDefault(c => c.ClrType == derivedType);

                            if (structuralType != null && structuralType.Kind == EdmTypeKind.Entity)
                            {
                                foundMappedTypes.Add((EntityTypeConfiguration)structuralType);
                            }

                            typeToBeVisited.Enqueue(derivedType);
                        }
                    }
                }
            }

            if (foundMappedTypes.Any())
            {
                this.ReconfigureEntityTypesAsComplexType(foundMappedTypes.ToArray());
            }
        }

        internal bool InferEdmTypeFromDerivedTypes(Type propertyType, ref PropertyKind propertyKind)
        {
            HashSet<Type> visitedTypes = new HashSet<Type>();

            Queue<Type> typeToBeVisited = new Queue<Type>();
            typeToBeVisited.Enqueue(propertyType);

            IList<StructuralTypeConfiguration> foundMappedTypes = new List<StructuralTypeConfiguration>();
            while (typeToBeVisited.Count != 0)
            {
                Type currentType = typeToBeVisited.Dequeue();
                visitedTypes.Add(currentType);

                List<Type> derivedTypes;
                if (this._allTypesWithDerivedTypeMapping.Value.TryGetValue(currentType, out derivedTypes))
                {
                    foreach (Type derivedType in derivedTypes)
                    {
                        if (!visitedTypes.Contains(derivedType))
                        {
                            StructuralTypeConfiguration structuralType =
                                this._explicitlyAddedTypes.FirstOrDefault(c => c.ClrType == derivedType);

                            if (structuralType != null)
                            {
                                foundMappedTypes.Add(structuralType);
                            }

                            typeToBeVisited.Enqueue(derivedType);
                        }
                    }
                }
            }

            if (!foundMappedTypes.Any())
            {
                return false;
            }

            IEnumerable<EntityTypeConfiguration> foundMappedEntityType =
                foundMappedTypes.OfType<EntityTypeConfiguration>().ToList();
            IEnumerable<ComplexTypeConfiguration> foundMappedComplexType =
                foundMappedTypes.OfType<ComplexTypeConfiguration>().ToList();

            if (!foundMappedEntityType.Any())
            {
                propertyKind = PropertyKind.Complex;
                return true;
            }
            else if (!foundMappedComplexType.Any())
            {
                propertyKind = PropertyKind.Navigation;
                return true;
            }
            else
            {
                throw Error.InvalidOperation(SRResources.CannotInferEdmType,
                    propertyType.FullName,
                    String.Join(",", foundMappedEntityType.Select(e => e.ClrType.FullName)),
                    String.Join(",", foundMappedComplexType.Select(e => e.ClrType.FullName)));
            }
        }

        // the convention model builder MapTypes() method might have went through deep object graphs and added a bunch of types
        // only to realise after applying the conventions that the user has ignored some of the properties. So, prune the unreachable stuff.
        private void PruneUnreachableTypes()
        {
            Contract.Assert(this._explicitlyAddedTypes != null);

            // Do a BFS starting with the types the user has explicitly added to find out the unreachable nodes.
            Queue<StructuralTypeConfiguration> reachableTypes = new Queue<StructuralTypeConfiguration>(this._explicitlyAddedTypes);
            HashSet<StructuralTypeConfiguration> visitedTypes = new HashSet<StructuralTypeConfiguration>();

            while (reachableTypes.Count != 0)
            {
                StructuralTypeConfiguration currentType = reachableTypes.Dequeue();

                // go visit other end of each of this node's edges.
                foreach (PropertyConfiguration property in currentType.Properties.Where(property => property.Kind != PropertyKind.Primitive))
                {
                    if (property.Kind == PropertyKind.Collection)
                    {
                        // if the elementType is primitive we don't need to do anything.
                        CollectionPropertyConfiguration colProperty = property as CollectionPropertyConfiguration;
                        if (EdmLibHelpers.GetEdmPrimitiveTypeOrNull(colProperty.ElementType) != null)
                        {
                            continue;
                        }
                    }

                    IEdmTypeConfiguration propertyType = this.GetStructuralTypeOrNull(property.RelatedClrType);
                    Contract.Assert(propertyType != null, "we should already have seen this type");

                    var structuralTypeConfiguration = propertyType as StructuralTypeConfiguration;
                    if (structuralTypeConfiguration != null && !visitedTypes.Contains(propertyType))
                    {
                        reachableTypes.Enqueue(structuralTypeConfiguration);
                    }
                }

                // all derived types and the base type are also reachable
                if (currentType.Kind == EdmTypeKind.Entity)
                {
                    EntityTypeConfiguration currentEntityType = (EntityTypeConfiguration)currentType;
                    if (currentEntityType.BaseType != null && !visitedTypes.Contains(currentEntityType.BaseType))
                    {
                        reachableTypes.Enqueue(currentEntityType.BaseType);
                    }

                    foreach (EntityTypeConfiguration derivedType in this.DerivedTypes(currentEntityType))
                    {
                        if (!visitedTypes.Contains(derivedType))
                        {
                            reachableTypes.Enqueue(derivedType);
                        }
                    }
                }
                else if (currentType.Kind == EdmTypeKind.Complex)
                {
                    ComplexTypeConfiguration currentComplexType = (ComplexTypeConfiguration)currentType;
                    if (currentComplexType.BaseType != null && !visitedTypes.Contains(currentComplexType.BaseType))
                    {
                        reachableTypes.Enqueue(currentComplexType.BaseType);
                    }

                    foreach (ComplexTypeConfiguration derivedType in this.DerivedTypes(currentComplexType))
                    {
                        if (!visitedTypes.Contains(derivedType))
                        {
                            reachableTypes.Enqueue(derivedType);
                        }
                    }
                }

                visitedTypes.Add(currentType);
            }

            StructuralTypeConfiguration[] allConfiguredTypes = this.StructuralTypes.ToArray();
            foreach (StructuralTypeConfiguration type in allConfiguredTypes)
            {
                if (!visitedTypes.Contains(type))
                {
                    // we don't have to fix up any properties because this type is unreachable and cannot be a property of any reachable type.
                    this.RemoveStructuralType(type.ClrType);
                }
            }
        }

        private void ApplyTypeAndPropertyConventions(StructuralTypeConfiguration edmTypeConfiguration)
        {
            foreach (IConvention convention in _conventions)
            {
                IEdmTypeConvention typeConvention = convention as IEdmTypeConvention;
                if (typeConvention != null)
                {
                    typeConvention.Apply(edmTypeConfiguration, this);
                }

                IEdmPropertyConvention propertyConvention = convention as IEdmPropertyConvention;
                if (propertyConvention != null)
                {
                    this.ApplyPropertyConvention(propertyConvention, edmTypeConfiguration);
                }
            }
        }

        private void ApplyEnumTypeConventions(EnumTypeConfiguration enumTypeConfiguration)
        {
            DataContractAttributeEnumTypeConvention typeConvention = new DataContractAttributeEnumTypeConvention();
            typeConvention.Apply(enumTypeConfiguration, this);
        }

        private void ApplyNavigationSourceConventions(NavigationSourceConfiguration navigationSourceConfiguration)
        {
            if (!this._configuredNavigationSources.Contains(navigationSourceConfiguration))
            {
                this._configuredNavigationSources.Add(navigationSourceConfiguration);

                foreach (INavigationSourceConvention convention in _conventions.OfType<INavigationSourceConvention>())
                {
                    if (convention != null)
                    {
                        convention.Apply(navigationSourceConfiguration, this);
                    }
                }
            }
        }

        private void ApplyOperationConventions(OperationConfiguration operation)
        {
            foreach (IOperationConvention convention in _conventions.OfType<IOperationConvention>())
            {
                convention.Apply(operation, this);
            }
        }

        private IEdmTypeConfiguration GetStructuralTypeOrNull(Type clrType)
        {
            IEdmTypeConfiguration configuration = this.StructuralTypes.SingleOrDefault(edmType => edmType.ClrType == clrType);
            if (configuration == null)
            {
                Type type = TypeHelper.GetUnderlyingTypeOrSelf(clrType);
                configuration = this.EnumTypes.SingleOrDefault(edmType => edmType.ClrType == type);
            }

            return configuration;
        }

        private void ApplyPropertyConvention(IEdmPropertyConvention propertyConvention, StructuralTypeConfiguration edmTypeConfiguration)
        {
            Contract.Assert(propertyConvention != null);
            Contract.Assert(edmTypeConfiguration != null);

            foreach (PropertyConfiguration property in edmTypeConfiguration.Properties.ToArray())
            {
                propertyConvention.Apply(property, edmTypeConfiguration, this);
            }
        }

        private void ReapplyPropertyConvention(PropertyConfiguration property,
            StructuralTypeConfiguration edmTypeConfiguration)
        {
            foreach (IEdmPropertyConvention propertyConvention in _conventions.OfType<IEdmPropertyConvention>())
            {
                  propertyConvention.Apply(property, edmTypeConfiguration, this);
            }
        }

        private static Dictionary<Type, List<Type>> BuildDerivedTypesMapping()
        {
            IEnumerable<Type> allTypes = TypeHelper.GetLoadedTypes().Where(t => t.IsVisible && t.IsClass && t != typeof(object));
            Dictionary<Type, List<Type>> allTypeMapping = allTypes.ToDictionary(k => k, k => new List<Type>());

            foreach (Type type in allTypes)
            {
                List<Type> derivedTypes;
                if (type.BaseType != null && allTypeMapping.TryGetValue(type.BaseType, out derivedTypes))
                {
                    derivedTypes.Add(type);
                }
            }

            return allTypeMapping;
        }

        /// <inheritdoc />
        public override void ValidateModel(IEdmModel model)
        {
            if (!this._isQueryCompositionMode)
            {
                base.ValidateModel(model);
            }
        }
    }
}
