// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Community.OData.Linq.OData.Query.Expressions
{
    using System;
    using System.Collections.Generic;

    using Community.OData.Linq.OData.Query;

    using Microsoft.OData.Edm;

    /// <summary>
    /// Represents the result of a $select and $expand query operation.
    /// </summary>
    public interface ISelectExpandWrapper
    {
        /// <summary>
        /// Projects the result of a $select and $expand query to a <see cref="IDictionary{TKey,TValue}" />.
        /// </summary>
        /// <returns>An <see cref="IDictionary{TKey,TValue}"/> representing the $select and $expand result.</returns>
        IDictionary<string, object> ToDictionary();

        /// <summary>
        /// Projects the result of a $select and/or $expand query to an <see cref="IDictionary{TKey,TValue}" /> using 
        /// the given <paramref name="propertyMapperProvider"/>. The <paramref name="propertyMapperProvider"/> is used 
        /// to obtain an <see cref="IEdmStructuredType"/> for the <see cref="ISelectExpandWrapper"/> that this 
        /// <see cref="IPropertyMapper"/> instance represents. This <see cref="ISelectExpandWrapper"/> will be used to 
        /// map the properties of the <see cref="IDictionary{TKey,TValue}"/> instance to the keys of the 
        /// returned <see cref="IEdmStructuredType"/>. This method can be used, for example, to map the property 
        /// names in the <see cref="IEdmStructuredType"/> to the names that should be used to serialize the properties 
        /// that this projection contains.
        /// </summary>
        /// <param name="propertyMapperProvider">
        /// A function that provides a new instance of an <see cref="IEdmStructuredType"/> for a given 
        /// <see cref="IEdmModel"/> and a given <see cref="IDictionary{TKey,TValue}"/>.
        /// </param>
        /// <returns>An <see cref="IPropertyMapper"/> representing the $select and $expand result.</returns>
        IDictionary<string, object> ToDictionary(Func<IEdmModel, IEdmStructuredType, IPropertyMapper> propertyMapperProvider);
    }
}
