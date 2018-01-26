// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Community.Data.OData.Linq.Builder
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    using Community.Data.OData.Linq.Common;
    using Community.Data.OData.Linq.Properties;

    using Microsoft.OData.Edm;

    // TODO: add support for FK properties
    // CUT: support for bi-directional properties

    /// <summary>
    /// Represents an <see cref="IEdmEntityType"/> that can be built using <see cref="ODataModelBuilder"/>.
    /// </summary>
    public class EntityTypeConfiguration : StructuralTypeConfiguration
    {
        private List<PrimitivePropertyConfiguration> _keys = new List<PrimitivePropertyConfiguration>();
        private List<EnumPropertyConfiguration> _enumKeys = new List<EnumPropertyConfiguration>();

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityTypeConfiguration"/> class.
        /// </summary>
        /// <remarks>The default constructor is intended for use by unit testing only.</remarks>
        public EntityTypeConfiguration()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityTypeConfiguration"/> class.
        /// </summary>
        /// <param name="modelBuilder">The <see cref="ODataModelBuilder"/> being used.</param>
        /// <param name="clrType">The backing CLR type for this entity type.</param>
        public EntityTypeConfiguration(ODataModelBuilder modelBuilder, Type clrType)
            : base(modelBuilder, clrType)
        {
        }

        /// <summary>
        /// Gets the <see cref="EdmTypeKind"/> of this <see cref="IEdmTypeConfiguration"/>
        /// </summary>
        public override EdmTypeKind Kind
        {
            get { return EdmTypeKind.Entity; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this type is a media type.
        /// </summary>
        public virtual bool HasStream { get; set; }

        /// <summary>
        /// Gets the collection of keys for this entity type.
        /// </summary>
        public virtual IEnumerable<PrimitivePropertyConfiguration> Keys
        {
            get
            {
                return this._keys;
            }
        }

        /// <summary>
        /// Gets the collection of enum keys for this entity type.
        /// </summary>
        public virtual IEnumerable<EnumPropertyConfiguration> EnumKeys
        {
            get { return this._enumKeys; }
        }

        /// <summary>
        /// Gets or sets the base type of this entity type.
        /// </summary>
        public virtual EntityTypeConfiguration BaseType
        {
            get
            {
                return this.BaseTypeInternal as EntityTypeConfiguration;
            }
            set
            {
                this.DerivesFrom(value);
            }
        }

        /// <summary>
        /// Marks this entity type as abstract.
        /// </summary>
        /// <returns>Returns itself so that multiple calls can be chained.</returns>
        public virtual EntityTypeConfiguration Abstract()
        {
            this.AbstractImpl();
            return this;
        }

        /// <summary>
        /// Marks this entity type as media type.
        /// </summary>
        /// <returns>Returns itself so that multiple calls can be chained.</returns>
        public virtual EntityTypeConfiguration MediaType()
        {
            this.HasStream = true;
            return this;
        }

        /// <summary>
        /// Configures the key property(s) for this entity type.
        /// </summary>
        /// <param name="keyProperty">The property to be added to the key properties of this entity type.</param>
        /// <returns>Returns itself so that multiple calls can be chained.</returns>
        public virtual EntityTypeConfiguration HasKey(PropertyInfo keyProperty)
        {
            if (this.BaseType != null && this.BaseType.Keys().Any())
            {
                throw Error.InvalidOperation(SRResources.CannotDefineKeysOnDerivedTypes, this.FullName, this.BaseType.FullName);
            }

            // Add the enum key if the property type is enum
            if (keyProperty.PropertyType.IsEnum)
            {
                this.ModelBuilder.AddEnumType(keyProperty.PropertyType);
                EnumPropertyConfiguration enumConfig = this.AddEnumProperty(keyProperty);

                // keys are always required
                enumConfig.IsRequired();

                if (!this._enumKeys.Contains(enumConfig))
                {
                    this._enumKeys.Add(enumConfig);
                }
            }
            else
            {
                PrimitivePropertyConfiguration propertyConfig = this.AddProperty(keyProperty);

                // keys are always required
                propertyConfig.IsRequired();

                if (!this._keys.Contains(propertyConfig))
                {
                    this._keys.Add(propertyConfig);
                }
            }

            return this;
        }

        /// <summary>
        /// Removes the property from the entity keys collection.
        /// </summary>
        /// <param name="keyProperty">The key to be removed.</param>
        /// <remarks>This method just disable the property to be not a key anymore. It does not remove the property all together.
        /// To remove the property completely, use the method <see cref="RemoveProperty"/></remarks>
        public virtual void RemoveKey(PrimitivePropertyConfiguration keyProperty)
        {
            if (keyProperty == null)
            {
                throw Error.ArgumentNull("keyProperty");
            }

            this._keys.Remove(keyProperty);
        }

        /// <summary>
        /// Removes the enum property from the entity enum keys collection.
        /// </summary>
        /// <param name="enumKeyProperty">The key to be removed.</param>
        /// <remarks>This method just disable the property to be not a key anymore. It does not remove the property all together.
        /// To remove the property completely, use the method <see cref="RemoveProperty"/></remarks>
        public virtual void RemoveKey(EnumPropertyConfiguration enumKeyProperty)
        {
            if (enumKeyProperty == null)
            {
                throw Error.ArgumentNull("enumKeyProperty");
            }

            this._enumKeys.Remove(enumKeyProperty);
        }

        /// <summary>
        /// Sets the base type of this entity type to <c>null</c> meaning that this entity type 
        /// does not derive from anything.
        /// </summary>
        /// <returns>Returns itself so that multiple calls can be chained.</returns>
        public virtual EntityTypeConfiguration DerivesFromNothing()
        {
            this.DerivesFromNothingImpl();
            return this;
        }

        /// <summary>
        /// Sets the base type of this entity type.
        /// </summary>
        /// <param name="baseType">The base entity type.</param>
        /// <returns>Returns itself so that multiple calls can be chained.</returns>
        public virtual EntityTypeConfiguration DerivesFrom(EntityTypeConfiguration baseType)
        {
            if ((this.Keys.Any() || this.EnumKeys.Any()) && baseType.Keys().Any())
            {
                throw Error.InvalidOperation(SRResources.CannotDefineKeysOnDerivedTypes, this.FullName, baseType.FullName);
            }

            this.DerivesFromImpl(baseType);
            return this;
        }

        /// <summary>
        /// Removes the property from the entity.
        /// </summary>
        /// <param name="propertyInfo">The <see cref="PropertyInfo"/> of the property to be removed.</param>
        public override void RemoveProperty(PropertyInfo propertyInfo)
        {
            base.RemoveProperty(propertyInfo);
            this._keys.RemoveAll(p => p.PropertyInfo == propertyInfo);
            this._enumKeys.RemoveAll(p => p.PropertyInfo == propertyInfo);
        }
    }
}
