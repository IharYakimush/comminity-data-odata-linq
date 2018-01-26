// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Community.OData.Linq.Builder
{
    using System;
    using System.Globalization;

    using Community.OData.Linq.Common;

    using Microsoft.OData.Edm;

    /// <summary>
    /// Represents a Collection of some named type.
    /// <example>
    /// Collection(Namespace.Customer) or Collection(Namespace.Address).
    /// </example>
    /// </summary>
    public class CollectionTypeConfiguration : IEdmTypeConfiguration
    {
        private IEdmTypeConfiguration _elementType;
        private Type _clrType;

        /// <summary>
        /// Constructs a collection that contains elements of the specified ElementType
        /// and that is represented in CLR using the specified clrType.
        /// </summary>
        /// <param name="elementType">The EdmTypeConfiguration of the elements in the collection</param>
        /// <param name="clrType">The type of this collection when manifested in CLR.</param>
        public CollectionTypeConfiguration(IEdmTypeConfiguration elementType, Type clrType)
        {
            if (elementType == null)
            {
                throw Error.ArgumentNull("elementType");
            }
            if (clrType == null)
            {
                throw Error.ArgumentNull("clrType");
            }
            this._elementType = elementType;
            this._clrType = clrType;
        }

        /// <summary>
        /// Gets the <see cref="IEdmTypeConfiguration" /> of elements in this collection.
        /// </summary>
        public IEdmTypeConfiguration ElementType
        {
            get { return this._elementType; }
        }

        /// <summary>
        /// Gets the CLR type associated with this collection type.
        /// </summary>
        public Type ClrType
        {
            get { return this._clrType; }
        }

        /// <summary>
        /// Gets the fullname (including namespace) of this collection type.
        /// </summary>
        public string FullName
        {
            get
            {
                // There is no need to include the Namespace when it comes from the Edm Namespace.
                return this.Name;
            }
        }

        /// <summary>
        /// Gets the namespace of this collection type.
        /// </summary>
        public string Namespace
        {
            get { return "Edm"; }
        }

        /// <summary>
        /// Gets the name of this collection type.
        /// </summary>
        public string Name
        {
            get { return String.Format(CultureInfo.InvariantCulture, "Collection({0})", this.ElementType.FullName); }
        }

        /// <summary>
        /// Gets the kind of the <see cref="IEdmType" />. In this case, it is <see cref="EdmTypeKind.Collection" />.
        /// </summary>
        public EdmTypeKind Kind
        {
            get { return EdmTypeKind.Collection; }
        }

        /// <summary>
        /// Gets the <see cref="ODataModelBuilder"/> used to create this configuration.
        /// </summary>
        public ODataModelBuilder ModelBuilder
        {
            get { return this._elementType.ModelBuilder; }
        }
    }
}
