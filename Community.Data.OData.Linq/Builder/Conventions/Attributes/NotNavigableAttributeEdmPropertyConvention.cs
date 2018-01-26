// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Community.OData.Linq.Builder.Conventions.Attributes
{
    using System;

    using Community.OData.Linq.Annotations;
    using Community.OData.Linq.Common;

    internal class NotNavigableAttributeEdmPropertyConvention : AttributeEdmPropertyConvention<NavigationPropertyConfiguration>
    {
        public NotNavigableAttributeEdmPropertyConvention()
            : base(attribute => attribute.GetType() == typeof(NotNavigableAttribute), allowMultiple: false)
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
                edmProperty.IsNotNavigable();
            }
        }
    }
}
