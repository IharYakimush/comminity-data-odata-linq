using System.Collections.Generic;

namespace Community.Data.OData.Linq.EdmModel
{
    using System;
    using System.Reflection;

    using Community.Data.OData.Linq.Builder;

    using Microsoft.OData.Edm;

    public static class Helper
    {
        public static IEdmModel Build(Type type)
        {
            ODataConventionModelBuilder builder = new ODataConventionModelBuilder();
            AddTypes(builder, type);
            
            return builder.GetEdmModel();
        }

        public static void AddTypes(ODataConventionModelBuilder builder, Type type)
        {
            builder.AddEntityType(type);
            builder.AddEntitySet(type.Name, new EntityTypeConfiguration(new ODataModelBuilder(), type));
        }

        public static EdmPrimitiveTypeKind GetEdmPrimitiveTypeKind(PropertyInfo property)
        {
            EdmPrimitiveTypeKind result = (EdmPrimitiveTypeKind)Enum.Parse(
                typeof(EdmPrimitiveTypeKind),
                property.PropertyType.Name,
                true);

            return result;
        }

        public static string GetPropertyName(PropertyInfo property)
        {
            return property.Name;
        }
    }
}