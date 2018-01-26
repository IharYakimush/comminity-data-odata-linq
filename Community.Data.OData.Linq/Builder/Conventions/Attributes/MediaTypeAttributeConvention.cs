// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Community.OData.Linq.Builder.Conventions.Attributes
{
    using System;

    using Community.OData.Linq.Common;

    internal class MediaTypeAttributeConvention : AttributeEdmTypeConvention<EntityTypeConfiguration>
    {
        public MediaTypeAttributeConvention()
            : base(attr => attr.GetType() == typeof(MediaTypeAttribute), false)
        {
        }

        public override void Apply(EntityTypeConfiguration edmTypeConfiguration, ODataConventionModelBuilder model,
            Attribute attribute)
        {
            if (edmTypeConfiguration == null)
            {
                throw Error.ArgumentNull("edmTypeConfiguration");
            }

            edmTypeConfiguration.MediaType();
        }
    }
}
