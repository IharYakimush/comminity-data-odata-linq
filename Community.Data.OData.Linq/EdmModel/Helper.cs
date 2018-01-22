namespace Community.Data.OData.Linq.EdmModel
{
    using System;
    using System.Reflection;

    using Microsoft.Data.Edm;
    using Microsoft.Data.Edm.Library;

    public static class Helper
    {
        public static IEdmModel Build(Type type)
        {
            EdmModel edmModel = new EdmModel();
            EdmEntityType edmEntityType = new EdmEntityType(type.Namespace, type.Name);
            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (property.PropertyType.IsPrimitive || property.PropertyType == typeof(string))
                {
                    string name = GetPropertyName(property);
                    EdmPrimitiveTypeKind kind = GetEdmPrimitiveTypeKind(property);
                    edmEntityType.AddStructuralProperty(name, kind);
                }
                else
                {
                    throw new NotImplementedException("Building model with non primitive properties currently not supported");
                }
            }
            edmModel.AddElement(edmEntityType);
            EdmEntityContainer edmEntityContainer = new EdmEntityContainer(type.Namespace, type.Name);
            edmEntityContainer.AddEntitySet(type.Name, edmEntityType);
            edmModel.AddElement(edmEntityContainer);

            return edmModel;
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