// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Community.OData.Linq.Builder.Validators
{
    using System.Collections.Generic;

    using Community.OData.Linq.Common;
    using Community.OData.Linq.OData.Query;
    using Community.OData.Linq.Properties;

    using Microsoft.OData;
    using Microsoft.OData.Edm;

    /// <summary>
    /// Represents a validator used to validate an <see cref="OrderByQueryOption"/> based on the <see cref="ODataValidationSettings"/>.
    /// </summary>
    public class OrderByQueryValidator
    {
        private readonly DefaultQuerySettings _defaultQuerySettings;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderByQueryValidator" /> class based on
        /// the <see cref="DefaultQuerySettings" />.
        /// </summary>
        /// <param name="defaultQuerySettings">The <see cref="DefaultQuerySettings" />.</param>
        public OrderByQueryValidator(DefaultQuerySettings defaultQuerySettings)
        {
            this._defaultQuerySettings = defaultQuerySettings;
        }

        /// <summary>
        /// Validates an <see cref="OrderByQueryOption" />.
        /// </summary>
        /// <param name="nodes">The $orderby query.</param>
        /// <param name="validationSettings">The validation settings.</param>
        public virtual void Validate(ICollection<OrderByNode> nodes, ODataValidationSettings validationSettings, IEdmModel model)
        {
            if (nodes.Count > validationSettings.MaxOrderByNodeCount)
            {
                throw new ODataException(Error.Format(SRResources.OrderByNodeCountExceeded,
                    validationSettings.MaxOrderByNodeCount));
            }

            OrderByModelLimitationsValidator validator =
                new OrderByModelLimitationsValidator(model, this._defaultQuerySettings.EnableOrderBy);
            bool explicitAllowedProperties = validationSettings.AllowedOrderByProperties.Count > 0;

            foreach (OrderByNode node in nodes)
            {
                string propertyName = null;
                OrderByPropertyNode propertyNode = node as OrderByPropertyNode;
                if (propertyNode != null)
                {
                    propertyName = propertyNode.Property.Name;
                    bool isValidPath = !validator.TryValidate(propertyNode.Property, propertyNode.Property.DeclaringType, propertyNode.OrderByClause, explicitAllowedProperties);
                    if (propertyName != null && isValidPath && explicitAllowedProperties)
                    {
                        // Explicit allowed properties were specified, but this one isn't within the list of allowed 
                        // properties.
                        if (!IsAllowed(validationSettings, propertyName))
                        {
                            throw new ODataException(Error.Format(SRResources.NotAllowedOrderByProperty, propertyName,
                                "AllowedOrderByProperties"));
                        }
                    }
                    else if (propertyName != null)
                    {
                        // The property wasn't limited but it wasn't contained in the set of explicitly allowed 
                        // properties.
                        if (!IsAllowed(validationSettings, propertyName))
                        {
                            throw new ODataException(Error.Format(SRResources.NotAllowedOrderByProperty, propertyName,
                                "AllowedOrderByProperties"));
                        }
                    }
                }
                else
                {
                    propertyName = "$it";
                    if (!IsAllowed(validationSettings, propertyName))
                    {
                        throw new ODataException(Error.Format(SRResources.NotAllowedOrderByProperty, propertyName,
                            "AllowedOrderByProperties"));
                    }
                }
            }
        }
        
        private static bool IsAllowed(ODataValidationSettings validationSettings, string propertyName)
        {
            return validationSettings.AllowedOrderByProperties.Count == 0 ||
                   validationSettings.AllowedOrderByProperties.Contains(propertyName);
        }
    }
}
