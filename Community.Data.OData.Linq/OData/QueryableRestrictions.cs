// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Community.OData.Linq.OData
{
    using Community.OData.Linq.Builder;

    /// <summary>
    /// Represents a queryable restriction on an EDM property, including not filterable, not sortable,
    /// not navigable, not expandable, not countable, automatically expand.
    /// </summary>
    public class QueryableRestrictions
    {
        private bool _autoExpand;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryableRestrictions"/> class.
        /// </summary>
        public QueryableRestrictions()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryableRestrictions"/> class.
        /// </summary>
        /// <param name="propertyConfiguration">The PropertyConfiguration containing queryable restrictions.</param>
        public QueryableRestrictions(PropertyConfiguration propertyConfiguration)
        {
            this.NotFilterable = propertyConfiguration.NotFilterable;
            this.NotSortable = propertyConfiguration.NotSortable;
            this.NotNavigable = propertyConfiguration.NotNavigable;
            this.NotExpandable = propertyConfiguration.NotExpandable;
            this.NotCountable = propertyConfiguration.NotCountable;
            this.DisableAutoExpandWhenSelectIsPresent = propertyConfiguration.DisableAutoExpandWhenSelectIsPresent;
            this._autoExpand = propertyConfiguration.AutoExpand;
        }

        /// <summary>
        /// Gets or sets whether the property is not filterable. default is false.
        /// </summary>
        public bool NotFilterable { get; set; }

        /// <summary>
        /// Gets or sets whether the property is nonfilterable. default is false.
        /// </summary>
        public bool NonFilterable
        {
            get { return this.NotFilterable; }
            set { this.NotFilterable = value; }
        }

        /// <summary>
        /// Gets or sets whether the property is not sortable. default is false.
        /// </summary>
        public bool NotSortable { get; set; }

        /// <summary>
        /// Gets or sets whether the property is unsortable. default is false.
        /// </summary>
        public bool Unsortable
        {
            get { return this.NotSortable; }
            set { this.NotSortable = value; }
        }

        /// <summary>
        /// Gets or sets whether the property is not navigable. default is false.
        /// </summary>
        public bool NotNavigable { get; set; }

        /// <summary>
        /// Gets or sets whether the property is not expandable. default is false.
        /// </summary>
        public bool NotExpandable { get; set; }

        /// <summary>
        /// Gets or sets whether the property is not countable. default is false.
        /// </summary>
        public bool NotCountable { get; set; }

        /// <summary>
        /// Gets or sets whether the property is automatically expanded. default is false.
        /// </summary>
        public bool AutoExpand 
        {
            get { return !this.NotExpandable && this._autoExpand; }
            set { this._autoExpand = value; }
        }

        /// <summary>
        /// If set to <c>true</c> then automatic expand will be disabled if there is a $select specify by client.
        /// </summary>
        public bool DisableAutoExpandWhenSelectIsPresent { get; set; }
    }
}
