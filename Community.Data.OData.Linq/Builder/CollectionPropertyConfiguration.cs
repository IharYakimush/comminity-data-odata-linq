// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Community.Data.OData.Linq.Builder
{
    using System;
    using System.Reflection;

    using Community.Data.OData.Linq.Common;
    using Community.Data.OData.Linq.OData;
    using Community.Data.OData.Linq.Properties;

    /// <summary>
    /// CollectionPropertyConfiguration represents a CollectionProperty on either an EntityType or ComplexType.
    /// </summary>
    public class CollectionPropertyConfiguration : StructuralPropertyConfiguration
    {
        private Type _elementType;

        /// <summary>
        /// Constructs a CollectionPropertyConfiguration using the <paramref name="property">property</paramref> provided.
        /// </summary>
        public CollectionPropertyConfiguration(PropertyInfo property, StructuralTypeConfiguration declaringType)
            : base(property, declaringType)
        {
            if (!property.PropertyType.IsCollection(out this._elementType))
            {
                throw Error.Argument("property", SRResources.CollectionPropertiesMustReturnIEnumerable, property.Name, property.DeclaringType.FullName);
            }
        }

        /// <inheritdoc />
        public override PropertyKind Kind
        {
            get { return PropertyKind.Collection; }
        }

        /// <inheritdoc />
        public override Type RelatedClrType
        {
            get { return this.ElementType; }
        }

        /// <summary>
        /// Returns the type of Elements in the Collection
        /// </summary>
        public Type ElementType
        {
            get { return this._elementType; }
        }

        /// <summary>
        /// Sets the CollectionProperty to optional (i.e. nullable).
        /// </summary>
        public CollectionPropertyConfiguration IsOptional()
        {
            this.OptionalProperty = true;
            return this;
        }

        /// <summary>
        /// Sets the CollectionProperty to required (i.e. non-nullable).
        /// </summary>
        public CollectionPropertyConfiguration IsRequired()
        {
            this.OptionalProperty = false;
            return this;
        }
    }
}
