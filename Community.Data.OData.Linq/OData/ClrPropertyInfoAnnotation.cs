// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Community.Data.OData.Linq.OData
{
    using System.Reflection;

    using Community.Data.OData.Linq.Common;

    using Microsoft.OData.Edm;

    /// <summary>
    /// Represents a mapping from an <see cref="IEdmProperty"/> to a CLR property info.
    /// </summary>
    public class ClrPropertyInfoAnnotation
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ClrPropertyInfoAnnotation"/> class.
        /// </summary>
        /// <param name="clrPropertyInfo">The backing CLR property info for the EDM property.</param>
        public ClrPropertyInfoAnnotation(PropertyInfo clrPropertyInfo)
        {
            if (clrPropertyInfo == null)
            {
                throw Error.ArgumentNull("clrPropertyInfo");
            }

            this.ClrPropertyInfo = clrPropertyInfo;
        }

        /// <summary>
        /// Gets the backing CLR property info for the EDM property.
        /// </summary>
        public PropertyInfo ClrPropertyInfo { get; private set; }
    }
}
