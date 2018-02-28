// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Community.OData.Linq.OData.Query.Expressions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using Community.OData.Linq.Common;
    using Community.OData.Linq.OData.Formatter;
    using Community.OData.Linq.OData.Query;
    using Community.OData.Linq.Properties;

    using Microsoft.OData.Edm;

    /// <summary>
    /// Represents a container class that contains properties that are either selected or expanded using $select and $expand.
    /// </summary>
    /// <typeparam name="TElement">The element being selected and expanded.</typeparam>
    //TODO: check converter [JsonConverter(typeof(SelectExpandWrapperConverter))]
    internal class SelectExpandWrapper<TElement> : IEdmEntityObject, ISelectExpandWrapper
    {
        private static readonly IPropertyMapper DefaultPropertyMapper = new IdentityPropertyMapper();

        private static readonly Func<IEdmModel, IEdmStructuredType, IPropertyMapper> _mapperProvider =
            (IEdmModel m, IEdmStructuredType t) => DefaultPropertyMapper;


        private Dictionary<string, object> _containerDict;
        private TypedEdmEntityObject _typedEdmEntityObject;

        /// <summary>
        /// Gets or sets the instance of the element being selected and expanded.
        /// </summary>
        public TElement Instance { get; set; }

        /// <summary>
        /// An ID to uniquely identify the model in the <see cref="ModelContainer"/>.
        /// </summary>
        public string ModelID { get; set; }

        /// <summary>
        /// Gets or sets the EDM type name of the element being selected and expanded. 
        /// </summary>
        /// <remarks>This is required by the <see cref="ODataMediaTypeFormatter"/> during serialization. If the instance property is not
        /// null, the type name will not be set as the type name can be figured from the instance runtime type.</remarks>
        public string TypeName { get; set; }

        /// <summary>
        /// Gets or sets the property container that contains the properties being expanded. 
        /// </summary>
        public PropertyContainer Container { get; set; }

        /// <inheritdoc />
        public IEdmTypeReference GetEdmType()
        {
            IEdmModel model = this.GetModel();

            if (this.TypeName != null)
            {
                IEdmEntityType entityType = model.FindDeclaredType(this.TypeName) as IEdmEntityType;
                if (entityType == null)
                {
                    throw Error.InvalidOperation(SRResources.ResourceTypeNotInModel, this.TypeName);
                }

                return new EdmEntityTypeReference(entityType, isNullable: false);
            }
            else
            {
                Type elementType = this.GetElementType();
                return model.GetEdmTypeReference(elementType);
            }
        }

        /// <inheritdoc />
        public bool TryGetPropertyValue(string propertyName, out object value)
        {
            // look into the container first to see if it has that property. container would have it 
            // if the property was expanded.
            if (this.Container != null)
            {
                this._containerDict = this._containerDict ?? this.Container.ToDictionary(DefaultPropertyMapper, includeAutoSelected: true);
                if (this._containerDict.TryGetValue(propertyName, out value))
                {
                    return true;
                }
            }

            // fall back to the instance.
            if (this.Instance != null)
            {
                this._typedEdmEntityObject = this._typedEdmEntityObject ??
                    new TypedEdmEntityObject(this.Instance, this.GetEdmType() as IEdmEntityTypeReference, this.GetModel());

                return this._typedEdmEntityObject.TryGetPropertyValue(propertyName, out value);
            }

            value = null;
            return false;
        }

        public IDictionary<string, object> ToDictionary()
        {
            return this.ToDictionary(_mapperProvider);
        }

        public IDictionary<string, object> ToDictionary(Func<IEdmModel, IEdmStructuredType, IPropertyMapper> mapperProvider)
        {
            if (mapperProvider == null)
            {
                throw Error.ArgumentNull("mapperProvider");
            }

            Dictionary<string, object> dictionary = new Dictionary<string, object>();
            IEdmStructuredType type = this.GetEdmType().AsStructured().StructuredDefinition();

            IPropertyMapper mapper = mapperProvider(this.GetModel(), type);
            if (mapper == null)
            {
                throw Error.InvalidOperation(SRResources.InvalidPropertyMapper, typeof(IPropertyMapper).FullName,
                    type.FullTypeName());
            }

            if (this.Container != null)
            {
                dictionary = this.Container.ToDictionary(mapper, includeAutoSelected: false);
            }

            // The user asked for all the structural properties on this instance.
            if (this.Instance != null)
            {
                foreach (IEdmStructuralProperty property in type.StructuralProperties())
                {
                    object propertyValue;
                    if (this.TryGetPropertyValue(property.Name, out propertyValue))
                    {
                        string mappingName = mapper.MapProperty(property.Name);
                        if (String.IsNullOrWhiteSpace(mappingName))
                        {
                            throw Error.InvalidOperation(SRResources.InvalidPropertyMapping, property.Name);
                        }

                        dictionary[mappingName] = propertyValue;
                    }
                }
            }

            return dictionary;
        }

        private Type GetElementType()
        {
            return this.Instance == null ? typeof(TElement) : this.Instance.GetType();
        }

        private IEdmModel GetModel()
        {
            Contract.Assert(this.ModelID != null);

            return ModelContainer.GetModel(this.ModelID);
        }
    }
}
