// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Community.Data.OData.Linq.Builder.Conventions
{
    internal interface IEdmTypeConvention : IConvention
    {
        void Apply(IEdmTypeConfiguration edmTypeConfiguration, ODataConventionModelBuilder model);
    }
}
