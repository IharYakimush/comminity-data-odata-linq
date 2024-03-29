﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Community.OData.Linq.Builder
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;

    using Community.OData.Linq.Common;
    using Community.OData.Linq.OData.Formatter;
    using Community.OData.Linq.Properties;

    using Microsoft.OData.Edm;

    /// <summary>
    /// <see cref="ODataModelBuilder"/> is used to map CLR classes to an EDM model.
    /// </summary>
    public class ODataModelBuilder
    {
        private static readonly Version _defaultDataServiceVersion = EdmConstants.EdmVersion4;
        private static readonly Version _defaultMaxDataServiceVersion = EdmConstants.EdmVersion4;

        private Dictionary<Type, EnumTypeConfiguration> _enumTypes = new Dictionary<Type, EnumTypeConfiguration>();
        private Dictionary<Type, StructuralTypeConfiguration> _structuralTypes = new Dictionary<Type, StructuralTypeConfiguration>();
        private Dictionary<string, NavigationSourceConfiguration> _navigationSources
            = new Dictionary<string, NavigationSourceConfiguration>();
        private Dictionary<Type, PrimitiveTypeConfiguration> _primitiveTypes = new Dictionary<Type, PrimitiveTypeConfiguration>();
        private List<OperationConfiguration> _operations = new List<OperationConfiguration>();

        private Version _dataServiceVersion;
        private Version _maxDataServiceVersion;

        /// <summary>
        /// Initializes a new instance of the <see cref="ODataModelBuilder"/> class.
        /// </summary>
        public ODataModelBuilder()
        {
            this.Namespace = "Default";
            this.ContainerName = "Container";
            this.DataServiceVersion = _defaultDataServiceVersion;
            this.MaxDataServiceVersion = _defaultMaxDataServiceVersion;
            this.BindingOptions = NavigationPropertyBindingOption.None;
        }

        /// <summary>
        /// Gets or sets the namespace that will be used for the resulting model
        /// </summary>
        public string Namespace { get; set; }

        /// <summary>
        /// Gets or sets the name of the container that will hold all the navigation sources, actions and functions
        /// </summary>
        public string ContainerName { get; set; }

        /// <summary>
        /// Gets or sets the data service version of the model. The default value is 4.0.
        /// </summary>
        public Version DataServiceVersion
        {
            get
            {
                return this._dataServiceVersion;
            }
            set
            {
                if (value == null)
                {
                    throw Error.PropertyNull();
                }
                this._dataServiceVersion = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum data service version of the model. The default value is 4.0.
        /// </summary>
        public Version MaxDataServiceVersion
        {
            get
            {
                return this._maxDataServiceVersion;
            }
            set
            {
                if (value == null)
                {
                    throw Error.PropertyNull();
                }
                this._maxDataServiceVersion = value;
            }
        }

        /// <summary>
        /// Gets the collection of EDM entity sets in the model to be built.
        /// </summary>
        public virtual IEnumerable<EntitySetConfiguration> EntitySets
        {
            get { return this._navigationSources.Values.OfType<EntitySetConfiguration>(); }
        }

        /// <summary>
        /// Gets the collection of EDM types in the model to be built.
        /// </summary>
        public virtual IEnumerable<StructuralTypeConfiguration> StructuralTypes
        {
            get { return this._structuralTypes.Values; }
        }

        /// <summary>
        /// Gets the collection of EDM types in the model to be built.
        /// </summary>
        public virtual IEnumerable<EnumTypeConfiguration> EnumTypes
        {
            get { return this._enumTypes.Values; }
        }

        /// <summary>
        /// Gets the collection of EDM singletons in the model to be built.
        /// </summary>
        public virtual IEnumerable<SingletonConfiguration> Singletons
        {
            get { return this._navigationSources.Values.OfType<SingletonConfiguration>(); }
        }

        /// <summary>
        /// Gets the collection of EDM navigation sources (entity sets and singletons) in the model to be built.
        /// </summary>
        public virtual IEnumerable<NavigationSourceConfiguration> NavigationSources
        {
            get { return this._navigationSources.Values; }
        }

        /// <summary>
        /// Gets the collection of Operations (i.e. Actions, Functions and ServiceOperations) in the model to be built.
        /// </summary>
        public virtual IEnumerable<OperationConfiguration> Operations
        {
            get { return this._operations; }
        }

        /// <summary>
        /// Gets/Sets the configured settings
        /// </summary>
        public ODataSettings ODataSettings { get; set; }

        /// <summary>
        /// Gets or sets the navigation property binding options.
        /// </summary>
        public NavigationPropertyBindingOption BindingOptions { get; set; }

        /// <summary>
        /// Registers an entity type as part of the model and returns an object that can be used to configure the entity type.
        /// This method can be called multiple times for the same entity to perform multiple lines of configuration.
        /// </summary>
        /// <typeparam name="TEntityType">The type to be registered or configured.</typeparam>
        /// <returns>The configuration object for the specified entity type.</returns>
        public EntityTypeConfiguration<TEntityType> EntityType<TEntityType>() where TEntityType : class
        {
            return new EntityTypeConfiguration<TEntityType>(this, this.AddEntityType(typeof(TEntityType)));
        }

        /// <summary>
        /// Registers a type as a complex type in the model and returns an object that can be used to configure the complex type.
        /// This method can be called multiple times for the same type to perform multiple lines of configuration.
        /// </summary>
        /// <typeparam name="TComplexType">The type to be registered or configured.</typeparam>
        /// <returns>The configuration object for the specified complex type.</returns>
        public ComplexTypeConfiguration<TComplexType> ComplexType<TComplexType>() where TComplexType : class
        {
            return new ComplexTypeConfiguration<TComplexType>(this, this.AddComplexType(typeof(TComplexType)));
        }

        /// <summary>
        /// Registers an entity set as a part of the model and returns an object that can be used to configure the entity set.
        /// This method can be called multiple times for the same type to perform multiple lines of configuration.
        /// </summary>
        /// <typeparam name="TEntityType">The entity type of the entity set.</typeparam>
        /// <param name="name">The name of the entity set.</param>
        /// <returns>The configuration object for the specified entity set.</returns>
        public EntitySetConfiguration<TEntityType> EntitySet<TEntityType>(string name) where TEntityType : class
        {
            EntityTypeConfiguration entity = this.AddEntityType(typeof(TEntityType));
            return new EntitySetConfiguration<TEntityType>(this, this.AddEntitySet(name, entity));
        }

        /// <summary>
        /// Registers an enum type as part of the model and returns an object that can be used to configure the enum.
        /// </summary>
        /// <typeparam name="TEnumType">The enum type to be registered or configured.</typeparam>
        /// <returns>The configuration object for the specified enum type.</returns>
        public EnumTypeConfiguration<TEnumType> EnumType<TEnumType>()
        {
            return new EnumTypeConfiguration<TEnumType>(this.AddEnumType(typeof(TEnumType)));
        }

        /// <summary>
        /// Registers a singleton as a part of the model and returns an object that can be used to configure the singleton.
        /// This method can be called multiple times for the same type to perform multiple lines of configuration.
        /// </summary>
        /// <typeparam name="TEntityType">The entity type of the singleton.</typeparam>
        /// <param name="name">The name of the singleton.</param>
        /// <returns>The configuration object for the specified singleton.</returns>
        public SingletonConfiguration<TEntityType> Singleton<TEntityType>(string name) where TEntityType : class
        {
            EntityTypeConfiguration entity = this.AddEntityType(typeof(TEntityType));
            return new SingletonConfiguration<TEntityType>(this, this.AddSingleton(name, entity));
        }

        /// <summary>
        /// Registers an entity type as part of the model and returns an object that can be used to configure the entity.
        /// This method can be called multiple times for the same entity to perform multiple lines of configuration.
        /// </summary>
        /// <param name="type">The type to be registered or configured.</param>
        /// <returns>The configuration object for the specified entity type.</returns>
        public virtual EntityTypeConfiguration AddEntityType(Type type)
        {
            if (type == null)
            {
                throw Error.ArgumentNull("type");
            }

            if (!this._structuralTypes.ContainsKey(type))
            {
                EntityTypeConfiguration entityTypeConfig = new EntityTypeConfiguration(this, type);
                this._structuralTypes.Add(type, entityTypeConfig);
                return entityTypeConfig;
            }
            else
            {
                EntityTypeConfiguration config = this._structuralTypes[type] as EntityTypeConfiguration;
                if (config == null || config.ClrType != type)
                {
                    throw Error.Argument("type", SRResources.TypeCannotBeEntityWasComplex, type.FullName);
                }

                return config;
            }
        }

        /// <summary>
        /// Registers an complex type as part of the model and returns an object that can be used to configure the entity.
        /// This method can be called multiple times for the same entity to perform multiple lines of configuration.
        /// </summary>
        /// <param name="type">The type to be registered or configured.</param>
        /// <returns>The configuration object for the specified complex type.</returns>
        public virtual ComplexTypeConfiguration AddComplexType(Type type)
        {
            if (type == null)
            {
                throw Error.ArgumentNull("type");
            }

            if (!this._structuralTypes.ContainsKey(type))
            {
                ComplexTypeConfiguration complexTypeConfig = new ComplexTypeConfiguration(this, type);
                this._structuralTypes.Add(type, complexTypeConfig);
                return complexTypeConfig;
            }
            else
            {
                ComplexTypeConfiguration complexTypeConfig = this._structuralTypes[type] as ComplexTypeConfiguration;
                if (complexTypeConfig == null || complexTypeConfig.ClrType != type)
                {
                    throw Error.Argument("type", SRResources.TypeCannotBeComplexWasEntity, type.FullName);
                }

                return complexTypeConfig;
            }
        }

        /// <summary>
        /// Registers an enum type as part of the model and returns an object that can be used to configure the enum type.
        /// </summary>
        /// <param name="type">The type to be registered or configured.</param>
        /// <returns>The configuration object for the specified enum type.</returns>
        public virtual EnumTypeConfiguration AddEnumType(Type type)
        {
            if (type == null)
            {
                throw Error.ArgumentNull("type");
            }

            if (!type.IsEnum)
            {
                throw Error.Argument("type", SRResources.TypeCannotBeEnum, type.FullName);
            }

            if (!this._enumTypes.ContainsKey(type))
            {
                EnumTypeConfiguration enumTypeConfig = new EnumTypeConfiguration(this, type);
                this._enumTypes.Add(type, enumTypeConfig);
                return enumTypeConfig;
            }
            else
            {
                EnumTypeConfiguration enumTypeConfig = this._enumTypes[type];
                if (enumTypeConfig.ClrType != type)
                {
                    throw Error.Argument("type", SRResources.TypeCannotBeEnum, type.FullName);
                }

                return enumTypeConfig;
            }
        }

        /// <summary>
        /// Adds a operation to the model.
        /// </summary>
        public virtual void AddOperation(OperationConfiguration operation)
        {
            this._operations.Add(operation);
        }

        /// <summary>
        /// Registers an entity set as a part of the model and returns an object that can be used to configure the entity set.
        /// This method can be called multiple times for the same type to perform multiple lines of configuration.
        /// </summary>
        /// <param name="name">The name of the entity set.</param>
        /// <param name="entityType">The type to be registered or configured.</param>
        /// <returns>The configuration object for the specified entity set.</returns>
        public virtual EntitySetConfiguration AddEntitySet(string name, EntityTypeConfiguration entityType)
        {
            if (String.IsNullOrWhiteSpace(name))
            {
                throw Error.ArgumentNullOrEmpty("name");
            }

            if (entityType == null)
            {
                throw Error.ArgumentNull("entityType");
            }

            if (name.Contains("."))
            {
                throw Error.NotSupported(SRResources.InvalidEntitySetName, name);
            }

            EntitySetConfiguration entitySet = null;
            if (this._navigationSources.ContainsKey(name))
            {
                entitySet = this._navigationSources[name] as EntitySetConfiguration;
                if (entitySet == null)
                {
                    throw Error.Argument("name", SRResources.EntitySetNameAlreadyConfiguredAsSingleton, name);
                }

                if (entitySet.EntityType != entityType)
                {
                    throw Error.Argument("entityType", SRResources.EntitySetAlreadyConfiguredDifferentEntityType,
                        entitySet.Name, entitySet.EntityType.Name);
                }
            }
            else
            {
                entitySet = new EntitySetConfiguration(this, entityType, name);
                this._navigationSources[name] = entitySet;
            }

            return entitySet;
        }

        /// <summary>
        /// Registers a singleton as a part of the model and returns an object that can be used to configure the singleton.
        /// This method can be called multiple times for the same type to perform multiple lines of configuration.
        /// </summary>
        /// <param name="name">The name of the singleton.</param>
        /// <param name="entityType">The type to be registered or configured.</param>
        /// <returns>The configuration object for the specified singleton.</returns>
        public virtual SingletonConfiguration AddSingleton(string name, EntityTypeConfiguration entityType)
        {
            if (String.IsNullOrWhiteSpace(name))
            {
                throw Error.ArgumentNullOrEmpty("name");
            }

            if (entityType == null)
            {
                throw Error.ArgumentNull("entityType");
            }

            if (name.Contains("."))
            {
                throw Error.NotSupported(SRResources.InvalidSingletonName, name);
            }

            SingletonConfiguration singleton = null;
            if (this._navigationSources.ContainsKey(name))
            {
                singleton = this._navigationSources[name] as SingletonConfiguration;
                if (singleton == null)
                {
                    throw Error.Argument("name", SRResources.SingletonNameAlreadyConfiguredAsEntitySet, name);
                }

                if (singleton.EntityType != entityType)
                {
                    throw Error.Argument("entityType", SRResources.SingletonAlreadyConfiguredDifferentEntityType,
                        singleton.Name, singleton.EntityType.Name);
                }
            }
            else
            {
                singleton = new SingletonConfiguration(this, entityType, name);
                this._navigationSources[name] = singleton;
            }

            return singleton;
        }

        /// <summary>
        /// Removes the type from the model.
        /// </summary>
        /// <param name="type">The type to be removed.</param>
        /// <returns><c>true</c> if the type is present in the model and <c>false</c> otherwise.</returns>
        public virtual bool RemoveStructuralType(Type type)
        {
            if (type == null)
            {
                throw Error.ArgumentNull("type");
            }

            return this._structuralTypes.Remove(type);
        }

        /// <summary>
        /// Removes the type from the model.
        /// </summary>
        /// <param name="type">The type to be removed.</param>
        /// <returns><c>true</c> if the type is present in the model and <c>false</c> otherwise.</returns>
        public virtual bool RemoveEnumType(Type type)
        {
            if (type == null)
            {
                throw Error.ArgumentNull("type");
            }

            return this._enumTypes.Remove(type);
        }

        /// <summary>
        /// Removes the entity set from the model.
        /// </summary>
        /// <param name="name">The name of the entity set to be removed.</param>
        /// <returns><c>true</c> if the entity set is present in the model and <c>false</c> otherwise.</returns>
        public virtual bool RemoveEntitySet(string name)
        {
            if (name == null)
            {
                throw Error.ArgumentNull("name");
            }

            if (this._navigationSources.ContainsKey(name))
            {
                EntitySetConfiguration entitySet = this._navigationSources[name] as EntitySetConfiguration;
                if (entitySet != null)
                {
                    return this._navigationSources.Remove(name);
                }
            }

            return false;
        }

        /// <summary>
        /// Removes the singleton from the model.
        /// </summary>
        /// <param name="name">The name of the singleton to be removed.</param>
        /// <returns><c>true</c> if the singleton is present in the model and <c>false</c> otherwise.</returns>
        public virtual bool RemoveSingleton(string name)
        {
            if (name == null)
            {
                throw Error.ArgumentNull("name");
            }

            if (this._navigationSources.ContainsKey(name))
            {
                SingletonConfiguration singleton = this._navigationSources[name] as SingletonConfiguration;
                if (singleton != null)
                {
                    return this._navigationSources.Remove(name);
                }
            }

            return false;
        }

        /// <summary>
        /// Remove the operation from the model
        /// <remarks>
        /// If there is more than one operation with the name specified this method will not work.
        /// You need to use the other RemoveOperation(..) overload instead.
        /// </remarks>
        /// </summary>
        /// <param name="name">The name of the operation to be removed.</param>
        /// <returns><c>true</c> if the operation is present in the model and <c>false</c> otherwise.</returns>
        public virtual bool RemoveOperation(string name)
        {
            if (name == null)
            {
                throw Error.ArgumentNull("name");
            }

            OperationConfiguration[] toRemove = this._operations.Where(p => p.Name == name).ToArray();
            int count = toRemove.Count();
            if (count == 1)
            {
                return this.RemoveOperation(toRemove[0]);
            }
            else if (count == 0)
            {
                // For consistency with RemoveStructuralType().
                // uses same semantics as Dictionary.Remove(key).
                return false;
            }
            else
            {
                throw Error.InvalidOperation(SRResources.MoreThanOneOperationFound, name);
            }
        }

        /// <summary>
        /// Remove the operation from the model
        /// </summary>
        /// <param name="operation">The operation to be removed.</param>
        /// <returns><c>true</c> if the operation is present in the model and <c>false</c> otherwise.</returns>
        public virtual bool RemoveOperation(OperationConfiguration operation)
        {
            if (operation == null)
            {
                throw Error.ArgumentNull("operation");
            }
            return this._operations.Remove(operation);
        }

        /// <summary>
        /// Attempts to find a pre-configured structural type or a primitive type or an enum type that matches the T.
        /// If no matches are found NULL is returned.
        /// </summary>
        public IEdmTypeConfiguration GetTypeConfigurationOrNull(Type type)
        {
            if (this._primitiveTypes.ContainsKey(type))
            {
                return this._primitiveTypes[type];
            }
            else
            {
                IEdmPrimitiveType edmType = EdmLibHelpers.GetEdmPrimitiveTypeOrNull(type);
                PrimitiveTypeConfiguration primitiveType = null;
                if (edmType != null)
                {
                    primitiveType = new PrimitiveTypeConfiguration(this, edmType, type);
                    this._primitiveTypes[type] = primitiveType;
                    return primitiveType;
                }
                else if (this._structuralTypes.ContainsKey(type))
                {
                    return this._structuralTypes[type];
                }
                else if (this._enumTypes.ContainsKey(type))
                {
                    return this._enumTypes[type];
                }
            }

            return null;
        }

        /// <summary>
        /// Creates a <see cref="IEdmModel"/> based on the configuration performed using this builder.
        /// </summary>
        /// <returns>The model that was built.</returns>
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "Property is not appropriate, method does work")]
        public virtual IEdmModel GetEdmModel()
        {
            IEdmModel model = EdmModelHelperMethods.BuildEdmModel(this);
            this.ValidateModel(model);
            return model;
        }

        /// <summary>
        /// Validates the <see cref="IEdmModel"/> that is being created.
        /// </summary>
        /// <param name="model">The <see cref="IEdmModel"/> that will be validated.</param>
        public virtual void ValidateModel(IEdmModel model)
        {
            if (model == null)
            {
                throw Error.ArgumentNull("model");
            }

            foreach (IEdmEntityType entity in model.SchemaElementsAcrossModels().OfType<IEdmEntityType>())
            {
                if (!entity.IsAbstract && !entity.Key().Any())
                {
                    throw Error.InvalidOperation(SRResources.EntityTypeDoesntHaveKeyDefined, entity.Name);
                }
            }

            foreach (IEdmNavigationSource navigationSource in model.EntityContainer.Elements.OfType<IEdmNavigationSource>())
            {
                if (!navigationSource.EntityType().Key().Any())
                {
                    throw Error.InvalidOperation(SRResources.NavigationSourceTypeHasNoKeys, navigationSource.Name,
                        navigationSource.EntityType().FullName());
                }
            }
        }
    }
}
