// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Community.Data.OData.Linq.Builder.Conventions.Attributes
{
    using System;

    using Community.Data.OData.Linq.Annotations;
    using Community.Data.OData.Linq.Common;

    internal class NonFilterableAttributeEdmPropertyConvention : AttributeEdmPropertyConvention<PropertyConfiguration>
    {
        public NonFilterableAttributeEdmPropertyConvention()
            : base(attribute => attribute.GetType() == typeof(NonFilterableAttribute), allowMultiple: false)
        {
        }

        public override void Apply(PropertyConfiguration edmProperty,
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
                edmProperty.IsNotFilterable();
            }
        }
    }
}
