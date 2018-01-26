// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Community.Data.OData.Linq.Builder.Conventions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Reflection;

    using Community.Data.OData.Linq.Common;
    using Community.Data.OData.Linq.OData;

    internal static class ConventionsHelpers
    {
        
        
        // Get properties of this structural type that are not already declared in the base structural type and are not already ignored.
        public static IEnumerable<PropertyInfo> GetProperties(StructuralTypeConfiguration structural, bool includeReadOnly)
        {
            IEnumerable<PropertyInfo> allProperties = GetAllProperties(structural, includeReadOnly);
            if (structural.BaseTypeInternal != null)
            {
                IEnumerable<PropertyInfo> baseTypeProperties = GetAllProperties(structural.BaseTypeInternal, includeReadOnly);
                return allProperties.Except(baseTypeProperties, PropertyEqualityComparer.Instance);
            }
            else
            {
                return allProperties;
            }
        }

        // Get all properties of this type (that are not already ignored).
        public static IEnumerable<PropertyInfo> GetAllProperties(StructuralTypeConfiguration type, bool includeReadOnly)
        {
            if (type == null)
            {
                throw Error.ArgumentNull("type");
            }

            return type
                .ClrType
                .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(p => p.IsValidStructuralProperty() && !type.IgnoredProperties().Any(p1 => p1.Name == p.Name)
                    && (includeReadOnly || p.GetSetMethod() != null || p.PropertyType.IsCollection()));
        }

        public static bool IsValidStructuralProperty(this PropertyInfo propertyInfo)
        {
            if (propertyInfo == null)
            {
                throw Error.ArgumentNull("propertyInfo");
            }

            // ignore any indexer properties.
            if (propertyInfo.GetIndexParameters().Any())
            {
                return false;
            }

            if (propertyInfo.CanRead)
            {
                // non-public getters are not valid properties
                MethodInfo publicGetter = propertyInfo.GetGetMethod();
                if (publicGetter != null && propertyInfo.PropertyType.IsValidStructuralPropertyType())
                {
                    return true;
                }
            }
            return false;
        }

        // Gets the ignored properties from this type and the base types.
        public static IEnumerable<PropertyInfo> IgnoredProperties(this StructuralTypeConfiguration structuralType)
        {
            if (structuralType == null)
            {
                return Enumerable.Empty<PropertyInfo>();
            }

            EntityTypeConfiguration entityType = structuralType as EntityTypeConfiguration;
            if (entityType != null)
            {
                return entityType.IgnoredProperties.Concat(entityType.BaseType.IgnoredProperties());
            }
            else
            {
                return structuralType.IgnoredProperties;
            }
        }

        public static bool IsValidStructuralPropertyType(this Type type)
        {
            if (type == null)
            {
                throw Error.ArgumentNull("type");
            }

            Type elementType;

            return !(type.IsGenericTypeDefinition
                     || type.IsPointer
                     || type == typeof(object)
                     || (type.IsCollection(out elementType) && elementType == typeof(object)));
        }
        
        private class PropertyEqualityComparer : IEqualityComparer<PropertyInfo>
        {
            public static PropertyEqualityComparer Instance = new PropertyEqualityComparer();

            public bool Equals(PropertyInfo x, PropertyInfo y)
            {
                Contract.Assert(x != null);
                Contract.Assert(y != null);

                return x.Name == y.Name;
            }

            public int GetHashCode(PropertyInfo obj)
            {
                Contract.Assert(obj != null);
                return obj.Name.GetHashCode();
            }
        }
    }
}
