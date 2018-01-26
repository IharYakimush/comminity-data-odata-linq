// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Community.Data.OData.Linq.Builder
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    using Community.Data.OData.Linq.OData;
    using Community.Data.OData.Linq.OData.Query;

    using Microsoft.OData.Edm;

    internal class EdmTypeMap
    {
        public EdmTypeMap(
            Dictionary<Type, IEdmType> edmTypes,
            Dictionary<PropertyInfo, IEdmProperty> edmProperties,
            Dictionary<IEdmProperty, QueryableRestrictions> edmPropertiesRestrictions,
            Dictionary<IEdmProperty, ModelBoundQuerySettings> edmPropertiesQuerySettings,
            Dictionary<IEdmStructuredType, ModelBoundQuerySettings> edmStructuredTypeQuerySettings,
            Dictionary<Enum, IEdmEnumMember> enumMembers,
            Dictionary<IEdmStructuredType, PropertyInfo> openTypes)
        {
            this.EdmTypes = edmTypes;
            this.EdmProperties = edmProperties;
            this.EdmPropertiesRestrictions = edmPropertiesRestrictions;
            this.EdmPropertiesQuerySettings = edmPropertiesQuerySettings;
            this.EdmStructuredTypeQuerySettings = edmStructuredTypeQuerySettings;
            this.EnumMembers = enumMembers;
            this.OpenTypes = openTypes;
        }

        public Dictionary<Type, IEdmType> EdmTypes { get; private set; }

        public Dictionary<PropertyInfo, IEdmProperty> EdmProperties { get; private set; }

        public Dictionary<IEdmProperty, QueryableRestrictions> EdmPropertiesRestrictions { get; private set; }

        public Dictionary<IEdmProperty, ModelBoundQuerySettings> EdmPropertiesQuerySettings { get; private set; }

        public Dictionary<IEdmStructuredType, ModelBoundQuerySettings> EdmStructuredTypeQuerySettings { get; private set; }

        public Dictionary<Enum, IEdmEnumMember> EnumMembers { get; private set; }

        public Dictionary<IEdmStructuredType, PropertyInfo> OpenTypes { get; private set; }
    }
}
