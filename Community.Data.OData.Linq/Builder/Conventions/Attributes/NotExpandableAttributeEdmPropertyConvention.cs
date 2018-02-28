using Community.OData.Linq.Annotations;
using Community.OData.Linq.Common;
using System;

namespace Community.OData.Linq.Builder.Conventions.Attributes
{
    internal class NotExpandableAttributeEdmPropertyConvention : AttributeEdmPropertyConvention<NavigationPropertyConfiguration>
    {
        public NotExpandableAttributeEdmPropertyConvention()
            : base(attribute => attribute.GetType() == typeof(NotExpandableAttribute), allowMultiple: false)
        {
        }

        public override void Apply(NavigationPropertyConfiguration edmProperty,
            StructuralTypeConfiguration structuralTypeConfiguration,
            Attribute attribute,
            ODataConventionModelBuilder model)
        {
            if (edmProperty == null)
            {
                throw Error.ArgumentNull("edmProperty");
            }

            if (!edmProperty.AddedExplicitly)
            {
                edmProperty.IsNotExpandable();
            }
        }
    }
}
