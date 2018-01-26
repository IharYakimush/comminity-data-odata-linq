// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Community.Data.OData.Linq.Builder
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Reflection;

    using Community.Data.OData.Linq.Common;
    using Community.Data.OData.Linq.OData;
    using Community.Data.OData.Linq.OData.Formatter;
    using Community.Data.OData.Linq.Properties;

    using Microsoft.OData.Edm;

    /// <summary>
    /// Represents the configuration for a navigation property of a structural type.
    /// </summary>
    /// <remarks>This configuration functionality is exposed by the model builder Fluent API, see <see cref="ODataModelBuilder"/>.</remarks>
    public class NavigationPropertyConfiguration : PropertyConfiguration
    {
        private readonly Type _relatedType;
        private readonly IDictionary<PropertyInfo, PropertyInfo> _referentialConstraint =
            new Dictionary<PropertyInfo, PropertyInfo>();

        /// <summary>
        /// Initializes a new instance of the <see cref="NavigationPropertyConfiguration"/> class.
        /// </summary>
        /// <param name="property">The backing CLR property.</param>
        /// <param name="multiplicity">The <see cref="EdmMultiplicity"/>.</param>
        /// <param name="declaringType">The declaring structural type.</param>
        public NavigationPropertyConfiguration(PropertyInfo property, EdmMultiplicity multiplicity, StructuralTypeConfiguration declaringType)
            : base(property, declaringType)
        {
            if (property == null)
            {
                throw Error.ArgumentNull("property");
            }

            this.Multiplicity = multiplicity;

            this._relatedType = property.PropertyType;
            if (multiplicity == EdmMultiplicity.Many)
            {
                Type elementType;
                if (!this._relatedType.IsCollection(out elementType))
                {
                    throw Error.Argument("property", SRResources.ManyToManyNavigationPropertyMustReturnCollection, property.Name, property.ReflectedType.Name);
                }

                this._relatedType = elementType;
            }

            this.OnDeleteAction = EdmOnDeleteAction.None;
        }

        /// <summary>
        /// Gets the <see cref="EdmMultiplicity"/> of this navigation property.
        /// </summary>
        public EdmMultiplicity Multiplicity { get; private set; }

        /// <summary>
        /// Gets whether this navigation property is a containment, default to false.
        /// </summary>
        public bool ContainsTarget { get; private set; }

        /// <summary>
        /// Gets the backing CLR type of this property type.
        /// </summary>
        public override Type RelatedClrType
        {
            get { return this._relatedType; }
        }

        /// <summary>
        /// Gets the <see cref="PropertyKind"/> of this property.
        /// </summary>
        public override PropertyKind Kind
        {
            get { return PropertyKind.Navigation; }
        }

        /// <summary>
        /// Gets or sets the delete action for this navigation property.
        /// </summary>
        public EdmOnDeleteAction OnDeleteAction { get; set; }

        /// <summary>
        /// Gets the foreign keys in the referential constraint of this navigation property.
        /// </summary>
        public IEnumerable<PropertyInfo> DependentProperties
        {
            get { return this._referentialConstraint.Keys; }
        }

        /// <summary>
        /// Gets the target keys in the referential constraint of this navigation property.
        /// </summary>
        public IEnumerable<PropertyInfo> PrincipalProperties
        {
            get { return this._referentialConstraint.Values; }
        }

        /// <summary>
        /// Marks the navigation property as optional.
        /// </summary>
        public NavigationPropertyConfiguration Optional()
        {
            if (this.Multiplicity == EdmMultiplicity.Many)
            {
                throw Error.InvalidOperation(SRResources.ManyNavigationPropertiesCannotBeChanged, this.Name);
            }

            this.Multiplicity = EdmMultiplicity.ZeroOrOne;
            return this;
        }

        /// <summary>
        /// Marks the navigation property as required.
        /// </summary>
        public NavigationPropertyConfiguration Required()
        {
            if (this.Multiplicity == EdmMultiplicity.Many)
            {
                throw Error.InvalidOperation(SRResources.ManyNavigationPropertiesCannotBeChanged, this.Name);
            }

            this.Multiplicity = EdmMultiplicity.One;
            return this;
        }

        /// <summary>
        /// Marks the navigation property as containment.
        /// </summary>
        public NavigationPropertyConfiguration Contained()
        {
            this.ContainsTarget = true;
            return this;
        }

        /// <summary>
        /// Marks the navigation property as non-contained.
        /// </summary>
        public NavigationPropertyConfiguration NonContained()
        {
            this.ContainsTarget = false;
            return this;
        }

        /// <summary>
        /// Marks the navigation property is automatic expanded.
        /// </summary>
        /// <param name="disableWhenSelectIsPresent">If set to <c>true</c> then automatic expand will be disabled
        /// if there is a $select specify by client.</param>
        /// <returns></returns>
        public NavigationPropertyConfiguration AutomaticallyExpand(bool disableWhenSelectIsPresent)
        {
            this.AutoExpand = true;
            this.DisableAutoExpandWhenSelectIsPresent = disableWhenSelectIsPresent;
            return this;
        }

        /// <summary>
        /// Configures cascade delete to be on for the navigation property.
        /// </summary>
        public NavigationPropertyConfiguration CascadeOnDelete()
        {
            this.CascadeOnDelete(cascade: true);
            return this;
        }

        /// <summary>
        /// Configures whether or not cascade delete is on for the navigation property.
        /// </summary>
        /// <param name="cascade"><c>true</c> indicates delete should also remove the associated items;
        /// <c>false</c> indicates no additional action on delete.</param>
        public NavigationPropertyConfiguration CascadeOnDelete(bool cascade)
        {
            this.OnDeleteAction = cascade ? EdmOnDeleteAction.Cascade : EdmOnDeleteAction.None;
            return this;
        }

        /// <summary>
        /// Configures the referential constraint with the specified <parameref name="dependentPropertyInfo"/>
        /// and <parameref name="principalPropertyInfo" />.
        /// </summary>
        /// <param name="dependentPropertyInfo">The dependent property info for the referential constraint.</param>
        /// <param name="principalPropertyInfo">The principal property info for the referential constraint.</param>
        public NavigationPropertyConfiguration HasConstraint(PropertyInfo dependentPropertyInfo,
            PropertyInfo principalPropertyInfo)
        {
            return this.HasConstraint(new KeyValuePair<PropertyInfo, PropertyInfo>(dependentPropertyInfo,
                principalPropertyInfo));
        }

        /// <summary>
        /// Configures the referential constraint with the dependent and principal property pair.
        /// </summary>
        /// <param name="constraint">The dependent and principal property pair.</param>
        public NavigationPropertyConfiguration HasConstraint(KeyValuePair<PropertyInfo, PropertyInfo> constraint)
        {
            if (constraint.Key == null)
            {
                throw Error.ArgumentNull("dependentPropertyInfo");
            }

            if (constraint.Value == null)
            {
                throw Error.ArgumentNull("principalPropertyInfo");
            }

            if (this.Multiplicity == EdmMultiplicity.Many)
            {
                throw Error.NotSupported(SRResources.ReferentialConstraintOnManyNavigationPropertyNotSupported,
                    this.Name, this.DeclaringType.ClrType.FullName);
            }

            if (this.ValidateConstraint(constraint))
            {
                return this;
            }

            EntityTypeConfiguration principalEntity = this.DeclaringType.ModelBuilder.StructuralTypes
                    .OfType<EntityTypeConfiguration>().FirstOrDefault(e => e.ClrType == this.RelatedClrType);
            Contract.Assert(principalEntity != null);

            PrimitivePropertyConfiguration principal = principalEntity.AddProperty(constraint.Value);
            PrimitivePropertyConfiguration dependent = this.DeclaringType.AddProperty(constraint.Key);

            // If the navigation property on which the referential constraint is defined or the principal property
            // is nullable, then the dependent property MUST be nullable.
            if (this.Multiplicity == EdmMultiplicity.ZeroOrOne || principal.OptionalProperty)
            {
                dependent.OptionalProperty = true;
            }

            // If both the navigation property and the principal property are not nullable,
            // then the dependent property MUST be marked with the Nullable="false" attribute value.
            if (this.Multiplicity == EdmMultiplicity.One && !principal.OptionalProperty)
            {
                dependent.OptionalProperty = false;
            }

            this._referentialConstraint.Add(constraint);
            return this;
        }

        private bool ValidateConstraint(KeyValuePair<PropertyInfo, PropertyInfo> constraint)
        {
            if (this._referentialConstraint.Contains(constraint))
            {
                return true;
            }

            PropertyInfo value;
            if (this._referentialConstraint.TryGetValue(constraint.Key, out value))
            {
                throw Error.InvalidOperation(SRResources.ReferentialConstraintAlreadyConfigured, "dependent",
                    constraint.Key.Name, "principal", value.Name);
            }

            if (this.PrincipalProperties.Any(p => p == constraint.Value))
            {
                PropertyInfo foundDependent = this._referentialConstraint.First(r => r.Value == constraint.Value).Key;

                throw Error.InvalidOperation(SRResources.ReferentialConstraintAlreadyConfigured, "principal",
                    constraint.Value.Name, "dependent", foundDependent.Name);
            }

            Type dependentType = Nullable.GetUnderlyingType(constraint.Key.PropertyType) ?? constraint.Key.PropertyType;
            Type principalType = Nullable.GetUnderlyingType(constraint.Value.PropertyType) ?? constraint.Value.PropertyType;

            // The principal property and the dependent property must have the same data type.
            if (dependentType != principalType)
            {
                throw Error.InvalidOperation(SRResources.DependentAndPrincipalTypeNotMatch,
                    constraint.Key.PropertyType.FullName, constraint.Value.PropertyType.FullName);
            }

            // OData V4 spec says that the principal and dependent property MUST be a path expression resolving to a primitive
            // property of the dependent entity type itself or to a primitive property of a complex property (recursively) of
            // the dependent entity type.
            // So far, ODL doesn't support to allow a primitive property of a complex property to be the dependent/principal property.
            // There's an issue tracking on: https://github.com/OData/odata.net/issues/22
            if (EdmLibHelpers.GetEdmPrimitiveTypeOrNull(constraint.Key.PropertyType) == null)
            {
                throw Error.InvalidOperation(SRResources.ReferentialConstraintPropertyTypeNotValid,
                    constraint.Key.PropertyType.FullName);
            }

            return false;
        }
    }
}
