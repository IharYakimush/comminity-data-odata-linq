// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Community.OData.Linq.Builder
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Reflection;

    using Community.OData.Linq.Common;
    using Community.OData.Linq.OData;
    using Community.OData.Linq.OData.Formatter;
    using Community.OData.Linq.Properties;

    using Microsoft.OData.Edm;

    /// <summary>
    /// Represents an <see cref="IEdmStructuredType"/> that can be built using <see cref="ODataModelBuilder"/>.
    /// </summary>
    public abstract class StructuralTypeConfiguration : IEdmTypeConfiguration
    {
        private const string DefaultNamespace = "Default";
        private string _namespace;
        private string _name;
        private PropertyInfo _dynamicPropertyDictionary;
        private StructuralTypeConfiguration _baseType;
        private bool _baseTypeConfigured;

        /// <summary>
        /// Initializes a new instance of the <see cref="StructuralTypeConfiguration"/> class.
        /// </summary>
        /// <remarks>The default constructor is intended for use by unit testing only.</remarks>
        protected StructuralTypeConfiguration()
        {
            this.ExplicitProperties = new Dictionary<PropertyInfo, PropertyConfiguration>();
            this.RemovedProperties = new List<PropertyInfo>();
            this.QueryConfiguration = new QueryConfiguration();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StructuralTypeConfiguration"/> class.
        /// </summary>
        /// <param name="clrType">The backing CLR type for this EDM structural type.</param>
        /// <param name="modelBuilder">The associated <see cref="ODataModelBuilder"/>.</param>
        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors", Justification = "The virtual property setters are only to support mocking frameworks, in which case this constructor shouldn't be called anyway.")]
        protected StructuralTypeConfiguration(ODataModelBuilder modelBuilder, Type clrType)
            : this()
        {
            if (modelBuilder == null)
            {
                throw Error.ArgumentNull("modelBuilder");
            }

            if (clrType == null)
            {
                throw Error.ArgumentNull("clrType");
            }

            this.ClrType = clrType;
            this.ModelBuilder = modelBuilder;
            this._name = clrType.EdmName();
            this._namespace = clrType.Namespace ?? DefaultNamespace;
        }

        /// <summary>
        /// Gets the <see cref="EdmTypeKind"/> of this edm type.
        /// </summary>
        public abstract EdmTypeKind Kind { get; }

        /// <summary>
        /// Gets the backing CLR <see cref="Type"/>.
        /// </summary>
        public virtual Type ClrType { get; private set; }

        /// <summary>
        /// Gets the full name of this edm type.
        /// </summary>
        public virtual string FullName
        {
            get
            {
                return this.Namespace + "." + this.Name;
            }
        }

        /// <summary>
        /// Gets or sets the namespace of this EDM type.
        /// </summary>
        public virtual string Namespace
        {
            get
            {
                return this._namespace;
            }
            set
            {
                if (value == null)
                {
                    throw Error.PropertyNull();
                }

                this._namespace = value;
                this.AddedExplicitly = true;
            }
        }

        /// <summary>
        /// Gets or sets the name of this EDM type.
        /// </summary>
        public virtual string Name
        {
            get
            {
                return this._name;
            }
            set
            {
                if (value == null)
                {
                    throw Error.PropertyNull();
                }

                this._name = value;
                this.AddedExplicitly = true;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this type is open or not.
        /// </summary>
        public bool IsOpen
        {
            get { return this._dynamicPropertyDictionary != null; }
        }

        /// <summary>
        /// Gets the CLR property info of the dynamic property dictionary on this structural type.
        /// </summary>
        public PropertyInfo DynamicPropertyDictionary
        {
            get { return this._dynamicPropertyDictionary; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this type is abstract.
        /// </summary>
        public virtual bool? IsAbstract { get; set; }

        /// <summary>
        /// Gets a value that represents whether the base type is explicitly configured or inferred.
        /// </summary>
        public virtual bool BaseTypeConfigured
        {
            get
            {
                return this._baseTypeConfigured;
            }
        }

        /// <summary>
        /// Gets the declared properties on this edm type.
        /// </summary>
        public IEnumerable<PropertyConfiguration> Properties
        {
            get
            {
                return this.ExplicitProperties.Values;
            }
        }

        /// <summary>
        /// Gets the properties from the backing CLR type that are to be ignored on this edm type.
        /// </summary>
        public ReadOnlyCollection<PropertyInfo> IgnoredProperties
        {
            get
            {
                return new ReadOnlyCollection<PropertyInfo>(this.RemovedProperties);
            }
        }

        /// <summary>
        /// Gets the collection of <see cref="NavigationPropertyConfiguration"/> of this entity type.
        /// </summary>
        public virtual IEnumerable<NavigationPropertyConfiguration> NavigationProperties
        {
            get
            {
                return this.ExplicitProperties.Values.OfType<NavigationPropertyConfiguration>();
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="QueryConfiguration"/>.
        /// </summary>
        public QueryConfiguration QueryConfiguration { get; set; }

        /// <summary>
        /// Gets or sets a value that is <c>true</c> if the type's name or namespace was set by the user; <c>false</c> if it was inferred through conventions.
        /// </summary>
        /// <remarks>The default value is <c>false</c>.</remarks>
        public bool AddedExplicitly { get; set; }

        /// <summary>
        /// The <see cref="ODataModelBuilder"/>.
        /// </summary>
        public virtual ODataModelBuilder ModelBuilder { get; private set; }

        /// <summary>
        /// Gets the collection of explicitly removed properties.
        /// </summary>
        protected internal IList<PropertyInfo> RemovedProperties { get; private set; }

        /// <summary>
        /// Gets the collection of explicitly added properties.
        /// </summary>
        protected internal IDictionary<PropertyInfo, PropertyConfiguration> ExplicitProperties { get; private set; }

        /// <summary>
        /// Gets the base type of this structural type.
        /// </summary>
        protected internal virtual StructuralTypeConfiguration BaseTypeInternal
        {
            get
            {
                return this._baseType;
            }
        }

        internal virtual void AbstractImpl()
        {
            this.IsAbstract = true;
        }

        internal virtual void DerivesFromNothingImpl()
        {
            this._baseType = null;
            this._baseTypeConfigured = true;
        }

        internal virtual void DerivesFromImpl(StructuralTypeConfiguration baseType)
        {
            if (baseType == null)
            {
                throw Error.ArgumentNull("baseType");
            }

            this._baseType = baseType;
            this._baseTypeConfigured = true;

            if (!baseType.ClrType.IsAssignableFrom(this.ClrType) || baseType.ClrType == this.ClrType)
            {
                throw Error.Argument("baseType", SRResources.TypeDoesNotInheritFromBaseType,
                    this.ClrType.FullName, baseType.ClrType.FullName);
            }

            foreach (PropertyConfiguration property in this.Properties)
            {
                this.ValidatePropertyNotAlreadyDefinedInBaseTypes(property.PropertyInfo);
            }

            foreach (PropertyConfiguration property in this.DerivedProperties())
            {
                this.ValidatePropertyNotAlreadyDefinedInDerivedTypes(property.PropertyInfo);
            }
        }

        /// <summary>
        /// Adds a primitive property to this edm type.
        /// </summary>
        /// <param name="propertyInfo">The property being added.</param>
        /// <returns>The <see cref="PrimitivePropertyConfiguration"/> so that the property can be configured further.</returns>
        public virtual PrimitivePropertyConfiguration AddProperty(PropertyInfo propertyInfo)
        {
            if (propertyInfo == null)
            {
                throw Error.ArgumentNull("propertyInfo");
            }

            if (!propertyInfo.ReflectedType.IsAssignableFrom(this.ClrType))
            {
                throw Error.Argument("propertyInfo", SRResources.PropertyDoesNotBelongToType, propertyInfo.Name, this.ClrType.FullName);
            }

            this.ValidatePropertyNotAlreadyDefinedInBaseTypes(propertyInfo);
            this.ValidatePropertyNotAlreadyDefinedInDerivedTypes(propertyInfo);

            // Remove from the ignored properties
            if (this.RemovedProperties.Any(prop => prop.Name.Equals(propertyInfo.Name)))
            {
                this.RemovedProperties.Remove(this.RemovedProperties.First(prop => prop.Name.Equals(propertyInfo.Name)));
            }

            PrimitivePropertyConfiguration propertyConfiguration =
                this.ValidatePropertyNotAlreadyDefinedOtherTypes<PrimitivePropertyConfiguration>(propertyInfo,
                    SRResources.MustBePrimitiveProperty);
            if (propertyConfiguration == null)
            {
                propertyConfiguration = new PrimitivePropertyConfiguration(propertyInfo, this);
                var primitiveType = EdmLibHelpers.GetEdmPrimitiveTypeOrNull(propertyInfo.PropertyType);
                if (primitiveType != null)
                {
                    if (primitiveType.PrimitiveKind == EdmPrimitiveTypeKind.Decimal)
                    {
                        propertyConfiguration = new DecimalPropertyConfiguration(propertyInfo, this);
                    }
                    else if (EdmLibHelpers.HasLength(primitiveType.PrimitiveKind))
                    {
                        propertyConfiguration = new LengthPropertyConfiguration(propertyInfo, this);
                    }
                    else if (EdmLibHelpers.HasPrecision(primitiveType.PrimitiveKind))
                    {
                        propertyConfiguration = new PrecisionPropertyConfiguration(propertyInfo, this);
                    }
                }
                this.ExplicitProperties[propertyInfo] = propertyConfiguration;
            }

            return propertyConfiguration;
        }

        /// <summary>
        /// Adds an enum property to this edm type.
        /// </summary>
        /// <param name="propertyInfo">The property being added.</param>
        /// <returns>The <see cref="EnumPropertyConfiguration"/> so that the property can be configured further.</returns>
        public virtual EnumPropertyConfiguration AddEnumProperty(PropertyInfo propertyInfo)
        {
            if (propertyInfo == null)
            {
                throw Error.ArgumentNull("propertyInfo");
            }

            if (!propertyInfo.ReflectedType.IsAssignableFrom(this.ClrType))
            {
                throw Error.Argument("propertyInfo", SRResources.PropertyDoesNotBelongToType, propertyInfo.Name, this.ClrType.FullName);
            }

            if (!TypeHelper.IsEnum(propertyInfo.PropertyType))
            {
                throw Error.Argument("propertyInfo", SRResources.MustBeEnumProperty, propertyInfo.Name, this.ClrType.FullName);
            }

            this.ValidatePropertyNotAlreadyDefinedInBaseTypes(propertyInfo);
            this.ValidatePropertyNotAlreadyDefinedInDerivedTypes(propertyInfo);

            // Remove from the ignored properties
            if (this.RemovedProperties.Any(prop => prop.Name.Equals(propertyInfo.Name)))
            {
                this.RemovedProperties.Remove(this.RemovedProperties.First(prop => prop.Name.Equals(propertyInfo.Name)));
            }

            EnumPropertyConfiguration propertyConfiguration =
                this.ValidatePropertyNotAlreadyDefinedOtherTypes<EnumPropertyConfiguration>(propertyInfo,
                    SRResources.MustBeEnumProperty);
            if (propertyConfiguration == null)
            {
                propertyConfiguration = new EnumPropertyConfiguration(propertyInfo, this);
                this.ExplicitProperties[propertyInfo] = propertyConfiguration;
            }

            return propertyConfiguration;
        }

        /// <summary>
        /// Adds a complex property to this edm type.
        /// </summary>
        /// <param name="propertyInfo">The property being added.</param>
        /// <returns>The <see cref="ComplexPropertyConfiguration"/> so that the property can be configured further.</returns>
        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "Helper validates non null propertyInfo")]
        public virtual ComplexPropertyConfiguration AddComplexProperty(PropertyInfo propertyInfo)
        {
            if (propertyInfo == null)
            {
                throw Error.ArgumentNull("propertyInfo");
            }

            if (!propertyInfo.ReflectedType.IsAssignableFrom(this.ClrType))
            {
                throw Error.Argument("propertyInfo", SRResources.PropertyDoesNotBelongToType, propertyInfo.Name, this.ClrType.FullName);
            }

            if (propertyInfo.PropertyType == this.ClrType)
            {
                throw Error.Argument("propertyInfo", SRResources.RecursiveComplexTypesNotAllowed, this.ClrType.FullName, propertyInfo.Name);
            }

            this.ValidatePropertyNotAlreadyDefinedInBaseTypes(propertyInfo);
            this.ValidatePropertyNotAlreadyDefinedInDerivedTypes(propertyInfo);

            // Remove from the ignored properties
            if (this.RemovedProperties.Any(prop => prop.Name.Equals(propertyInfo.Name)))
            {
                this.RemovedProperties.Remove(this.RemovedProperties.First(prop => prop.Name.Equals(propertyInfo.Name)));
            }

            ComplexPropertyConfiguration propertyConfiguration =
                this.ValidatePropertyNotAlreadyDefinedOtherTypes<ComplexPropertyConfiguration>(propertyInfo,
                    SRResources.MustBeComplexProperty);
            if (propertyConfiguration == null)
            {
                propertyConfiguration = new ComplexPropertyConfiguration(propertyInfo, this);
                this.ExplicitProperties[propertyInfo] = propertyConfiguration;
                // Make sure the complex type is in the model.

                this.ModelBuilder.AddComplexType(propertyInfo.PropertyType);
            }

            return propertyConfiguration;
        }

        /// <summary>
        /// Adds a collection property to this edm type.
        /// </summary>
        /// <param name="propertyInfo">The property being added.</param>
        /// <returns>The <see cref="CollectionPropertyConfiguration"/> so that the property can be configured further.</returns>
        public virtual CollectionPropertyConfiguration AddCollectionProperty(PropertyInfo propertyInfo)
        {
            if (propertyInfo == null)
            {
                throw Error.ArgumentNull("propertyInfo");
            }

            if (!propertyInfo.DeclaringType.IsAssignableFrom(this.ClrType))
            {
                throw Error.Argument("propertyInfo", SRResources.PropertyDoesNotBelongToType);
            }

            this.ValidatePropertyNotAlreadyDefinedInBaseTypes(propertyInfo);
            this.ValidatePropertyNotAlreadyDefinedInDerivedTypes(propertyInfo);

            // Remove from the ignored properties
            if (this.RemovedProperties.Any(prop => prop.Name.Equals(propertyInfo.Name)))
            {
                this.RemovedProperties.Remove(this.RemovedProperties.First(prop => prop.Name.Equals(propertyInfo.Name)));
            }

            CollectionPropertyConfiguration propertyConfiguration =
                this.ValidatePropertyNotAlreadyDefinedOtherTypes<CollectionPropertyConfiguration>(propertyInfo,
                    SRResources.MustBeCollectionProperty);
            if (propertyConfiguration == null)
            {
                propertyConfiguration = new CollectionPropertyConfiguration(propertyInfo, this);
                this.ExplicitProperties[propertyInfo] = propertyConfiguration;

                // If the ElementType is the same as this type this is recursive complex type nesting
                if (propertyConfiguration.ElementType == this.ClrType)
                {
                    throw Error.Argument("propertyInfo",
                        SRResources.RecursiveComplexTypesNotAllowed,
                        this.ClrType.Name,
                        propertyConfiguration.Name);
                }

                // If the ElementType is not primitive or enum treat as a ComplexType and Add to the model.
                IEdmPrimitiveTypeReference edmType =
                    EdmLibHelpers.GetEdmPrimitiveTypeReferenceOrNull(propertyConfiguration.ElementType);
                if (edmType == null)
                {
                    if (!TypeHelper.IsEnum(propertyConfiguration.ElementType))
                    {
                        this.ModelBuilder.AddComplexType(propertyConfiguration.ElementType);
                    }
                }
            }

            return propertyConfiguration;
        }

        /// <summary>
        /// Adds the property info of the dynamic properties to this structural type.
        /// </summary>
        /// <param name="propertyInfo">The property being added.</param>
        public virtual void AddDynamicPropertyDictionary(PropertyInfo propertyInfo)
        {
            if (propertyInfo == null)
            {
                throw Error.ArgumentNull("propertyInfo");
            }

            if (!typeof(IDictionary<string, object>).IsAssignableFrom(propertyInfo.PropertyType))
            {
                throw Error.Argument("propertyInfo", SRResources.ArgumentMustBeOfType,
                    "IDictionary<string, object>");
            }

            if (!propertyInfo.DeclaringType.IsAssignableFrom(this.ClrType))
            {
                throw Error.Argument("propertyInfo", SRResources.PropertyDoesNotBelongToType);
            }

            // Remove from the ignored properties
            if (this.IgnoredProperties.Contains(propertyInfo))
            {
                this.RemovedProperties.Remove(propertyInfo);
            }

            if (this._dynamicPropertyDictionary != null)
            {
                throw Error.Argument("propertyInfo", SRResources.MoreThanOneDynamicPropertyContainerFound, this.ClrType.Name);
            }

            this._dynamicPropertyDictionary = propertyInfo;
        }

        /// <summary>
        /// Removes the given property.
        /// </summary>
        /// <param name="propertyInfo">The property being removed.</param>
        public virtual void RemoveProperty(PropertyInfo propertyInfo)
        {
            if (propertyInfo == null)
            {
                throw Error.ArgumentNull("propertyInfo");
            }

            if (!propertyInfo.ReflectedType.IsAssignableFrom(this.ClrType))
            {
                throw Error.Argument("propertyInfo", SRResources.PropertyDoesNotBelongToType, propertyInfo.Name, this.ClrType.FullName);
            }

            if (this.ExplicitProperties.Keys.Any(key => key.Name.Equals(propertyInfo.Name)))
            {
                this.ExplicitProperties.Remove(this.ExplicitProperties.Keys.First(key => key.Name.Equals(propertyInfo.Name)));
            }

            if (!this.RemovedProperties.Any(prop => prop.Name.Equals(propertyInfo.Name)))
            {
                this.RemovedProperties.Add(propertyInfo);
            }

            if (this._dynamicPropertyDictionary == propertyInfo)
            {
                this._dynamicPropertyDictionary = null;
            }
        }

        /// <summary>
        /// Adds a non-contained EDM navigation property to this entity type.
        /// </summary>
        /// <param name="navigationProperty">The backing CLR property.</param>
        /// <param name="multiplicity">The <see cref="EdmMultiplicity"/> of the navigation property.</param>
        /// <returns>Returns the <see cref="NavigationPropertyConfiguration"/> of the added property.</returns>
        public virtual NavigationPropertyConfiguration AddNavigationProperty(PropertyInfo navigationProperty, EdmMultiplicity multiplicity)
        {
            return this.AddNavigationProperty(navigationProperty, multiplicity, containsTarget: false);
        }

        /// <summary>
        /// Adds a contained EDM navigation property to this entity type.
        /// </summary>
        /// <param name="navigationProperty">The backing CLR property.</param>
        /// <param name="multiplicity">The <see cref="EdmMultiplicity"/> of the navigation property.</param>
        /// <returns>Returns the <see cref="NavigationPropertyConfiguration"/> of the added property.</returns>
        public virtual NavigationPropertyConfiguration AddContainedNavigationProperty(PropertyInfo navigationProperty, EdmMultiplicity multiplicity)
        {
            return this.AddNavigationProperty(navigationProperty, multiplicity, containsTarget: true);
        }

        private NavigationPropertyConfiguration AddNavigationProperty(PropertyInfo navigationProperty, EdmMultiplicity multiplicity, bool containsTarget)
        {
            if (navigationProperty == null)
            {
                throw Error.ArgumentNull("navigationProperty");
            }

            if (!navigationProperty.ReflectedType.IsAssignableFrom(this.ClrType))
            {
                throw Error.Argument("navigationProperty", SRResources.PropertyDoesNotBelongToType, navigationProperty.Name, this.ClrType.FullName);
            }

            this.ValidatePropertyNotAlreadyDefinedInBaseTypes(navigationProperty);
            this.ValidatePropertyNotAlreadyDefinedInDerivedTypes(navigationProperty);

            PropertyConfiguration propertyConfig;
            NavigationPropertyConfiguration navigationPropertyConfig;

            if (this.ExplicitProperties.ContainsKey(navigationProperty))
            {
                propertyConfig = this.ExplicitProperties[navigationProperty];
                if (propertyConfig.Kind != PropertyKind.Navigation)
                {
                    throw Error.Argument("navigationProperty", SRResources.MustBeNavigationProperty, navigationProperty.Name, this.ClrType.FullName);
                }

                navigationPropertyConfig = propertyConfig as NavigationPropertyConfiguration;
                if (navigationPropertyConfig.Multiplicity != multiplicity)
                {
                    throw Error.Argument("navigationProperty", SRResources.MustHaveMatchingMultiplicity, navigationProperty.Name, multiplicity);
                }
            }
            else
            {
                navigationPropertyConfig = new NavigationPropertyConfiguration(
                    navigationProperty,
                    multiplicity,
                    this);
                if (containsTarget)
                {
                    navigationPropertyConfig = navigationPropertyConfig.Contained();
                }

                this.ExplicitProperties[navigationProperty] = navigationPropertyConfig;
                // make sure the related type is configured
                this.ModelBuilder.AddEntityType(navigationPropertyConfig.RelatedClrType);
            }
            return navigationPropertyConfig;
        }

        internal T ValidatePropertyNotAlreadyDefinedOtherTypes<T>(PropertyInfo propertyInfo, string typeErrorMessage) where T : class
        {
            T propertyConfiguration = default(T);
            var explicitPropertyInfo = this.ExplicitProperties.Keys.FirstOrDefault(key => key.Name.Equals(propertyInfo.Name));
            if (explicitPropertyInfo != null)
            {
                propertyConfiguration = this.ExplicitProperties[explicitPropertyInfo] as T;
                if (propertyConfiguration == default(T))
                {
                    throw Error.Argument("propertyInfo", typeErrorMessage, propertyInfo.Name, this.ClrType.FullName);
                }
            }

            return propertyConfiguration;
        }

        internal void ValidatePropertyNotAlreadyDefinedInBaseTypes(PropertyInfo propertyInfo)
        {
            PropertyConfiguration baseProperty =
                this.DerivedProperties().FirstOrDefault(p => p.Name == propertyInfo.Name);
            if (baseProperty != null)
            {
                throw Error.Argument("propertyInfo", SRResources.CannotRedefineBaseTypeProperty,
                    propertyInfo.Name, baseProperty.PropertyInfo.ReflectedType.FullName);
            }
        }

        internal void ValidatePropertyNotAlreadyDefinedInDerivedTypes(PropertyInfo propertyInfo)
        {
            foreach (StructuralTypeConfiguration derivedType in this.ModelBuilder.DerivedTypes(this))
            {
                PropertyConfiguration propertyInDerivedType =
                    derivedType.Properties.FirstOrDefault(p => p.Name == propertyInfo.Name);
                if (propertyInDerivedType != null)
                {
                    throw Error.Argument("propertyInfo", SRResources.PropertyAlreadyDefinedInDerivedType,
                        propertyInfo.Name, this.FullName, derivedType.FullName);
                }
            }
        }
    }
}
