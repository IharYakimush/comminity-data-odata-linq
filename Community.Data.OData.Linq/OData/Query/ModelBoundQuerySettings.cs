// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Community.Data.OData.Linq.OData.Query
{
    using System.Collections.Generic;

    using Community.Data.OData.Linq.Common;

    /// <summary>
    /// This class describes the model bound settings to use during query composition.
    /// </summary>
    public class ModelBoundQuerySettings
    {
        private int? _pageSize;
        private int? _maxTop = 0;
        private Dictionary<string, ExpandConfiguration> _expandConfigurations = new Dictionary<string, ExpandConfiguration>();
        private Dictionary<string, SelectExpandType> _selectConfigurations = new Dictionary<string, SelectExpandType>();
        private Dictionary<string, bool> _orderByConfigurations = new Dictionary<string, bool>();
        private Dictionary<string, bool> _filterConfigurations = new Dictionary<string, bool>();

        internal static ModelBoundQuerySettings DefaultModelBoundQuerySettings = new ModelBoundQuerySettings
        {
            _maxTop = 0
        };

        /// <summary>
        /// Instantiates a new instance of the <see cref="ModelBoundQuerySettings"/> class
        /// </summary>
        public ModelBoundQuerySettings()
        {
        }

        /// <summary>
        /// Copy and create new instance of the <see cref="ModelBoundQuerySettings"/> class 
        /// </summary>
        public ModelBoundQuerySettings(ModelBoundQuerySettings querySettings)
        {
            this._maxTop = querySettings.MaxTop;
            this.PageSize = querySettings.PageSize;
            this.Countable = querySettings.Countable;
            this.DefaultEnableFilter = querySettings.DefaultEnableFilter;
            this.DefaultEnableOrderBy = querySettings.DefaultEnableOrderBy;
            this.DefaultExpandType = querySettings.DefaultExpandType;
            this.DefaultMaxDepth = querySettings.DefaultMaxDepth;
            this.DefaultSelectType = querySettings.DefaultSelectType;
            this.CopyOrderByConfigurations(querySettings.OrderByConfigurations);
            this.CopyFilterConfigurations(querySettings.FilterConfigurations);
            this.CopyExpandConfigurations(querySettings.ExpandConfigurations);
            this.CopySelectConfigurations(querySettings.SelectConfigurations);
        }

        /// <summary>
        /// Gets or sets the max value of $top that a client can request.
        /// </summary>
        /// <value>
        /// The max value of $top that a client can request, or <c>null</c> if there is no limit.
        /// </value>
        public int? MaxTop
        {
            get
            {
                return this._maxTop;
            }
            set
            {
                if (value.HasValue && value <= 0)
                {
                    throw Error.ArgumentMustBeGreaterThanOrEqualTo("value", value, 0);
                }

                this._maxTop = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum number of query results to return.
        /// </summary>
        /// <value>
        /// The maximum number of query results to return, or <c>null</c> if there is no limit.
        /// </value>
        public int? PageSize
        {
            get
            {
                return this._pageSize;
            }
            set
            {
                if (value.HasValue && value <= 0)
                {
                    throw Error.ArgumentMustBeGreaterThanOrEqualTo("value", value, 1);
                }

                this._pageSize = value;
            }
        }

        /// <summary>
        /// Represents the $count can be applied or not.
        /// </summary>
        public bool? Countable { get; set; }

        /// <summary>
        /// Gets the <see cref="ExpandConfiguration"/>s of navigation properties.
        /// </summary>
        public Dictionary<string, ExpandConfiguration> ExpandConfigurations
        {
            get
            {
                return this._expandConfigurations;
            }
        }

        /// <summary>
        /// Gets or sets the default <see cref="SelectExpandType"/> of navigation properties.
        /// </summary>
        public SelectExpandType? DefaultExpandType { get; set; }

        /// <summary>
        /// Gets or sets the default maxDepth of navigation properties.
        /// </summary>
        public int DefaultMaxDepth { get; set; }

        /// <summary>
        /// Gets or sets whether the properties can apply $orderby by default.
        /// </summary>
        public bool? DefaultEnableOrderBy { get; set; }

        /// <summary>
        /// Gets or sets whether the properties can apply $filter by default.
        /// </summary>
        public bool? DefaultEnableFilter { get; set; }

        /// <summary>
        /// Gets or sets whether the properties can apply $select by default.
        /// </summary>
        public SelectExpandType? DefaultSelectType { get; set; }

        /// <summary>
        /// Gets the $orderby configuration of properties.
        /// </summary>
        public Dictionary<string, bool> OrderByConfigurations
        {
            get
            {
                return this._orderByConfigurations;
            }
        }

        /// <summary>
        /// Gets the $filter configuration of properties.
        /// </summary>
        public Dictionary<string, bool> FilterConfigurations
        {
            get
            {
                return this._filterConfigurations;
            }
        }

        /// <summary>
        /// Gets the $select configuration of properties.
        /// </summary>
        public Dictionary<string, SelectExpandType> SelectConfigurations
        {
            get
            {
                return this._selectConfigurations;
            }
        }

        /// <summary>
        /// Copy the <see cref="ExpandConfiguration"/>s of navigation properties.
        /// </summary>
        internal void CopyExpandConfigurations(Dictionary<string, ExpandConfiguration> expandConfigurations)
        {
            this._expandConfigurations.Clear();
            foreach (var expandConfiguration in expandConfigurations)
            {
                this._expandConfigurations.Add(expandConfiguration.Key, expandConfiguration.Value);
            }
        }

        /// <summary>
        /// Copy the $orderby configuration of properties.
        /// </summary>
        internal void CopyOrderByConfigurations(Dictionary<string, bool> orderByConfigurations)
        {
            this._orderByConfigurations.Clear();
            foreach (var orderByConfiguration in orderByConfigurations)
            {
                this._orderByConfigurations.Add(orderByConfiguration.Key, orderByConfiguration.Value);
            }
        }

        /// <summary>
        /// Copy the $select configuration of properties.
        /// </summary>
        internal void CopySelectConfigurations(Dictionary<string, SelectExpandType> selectConfigurations)
        {
            this._selectConfigurations.Clear();
            foreach (var selectConfiguration in selectConfigurations)
            {
                this._selectConfigurations.Add(selectConfiguration.Key, selectConfiguration.Value);
            }
        }

        /// <summary>
        /// Copy the $filter configuration of properties.
        /// </summary>
        internal void CopyFilterConfigurations(Dictionary<string, bool> filterConfigurations)
        {
            this._filterConfigurations.Clear();
            foreach (var filterConfiguration in filterConfigurations)
            {
                this._filterConfigurations.Add(filterConfiguration.Key, filterConfiguration.Value);
            }
        }

        internal bool Expandable(string propertyName)
        {
            ExpandConfiguration expandConfiguration;
            if (this.ExpandConfigurations.TryGetValue(propertyName, out expandConfiguration))
            {
                return expandConfiguration.ExpandType != SelectExpandType.Disabled;
            }
            else
            {
                return this.DefaultExpandType.HasValue && this.DefaultExpandType != SelectExpandType.Disabled;
            }
        }

        internal bool Selectable(string propertyName)
        {
            SelectExpandType selectExpandType;
            if (this.SelectConfigurations.TryGetValue(propertyName, out selectExpandType))
            {
                return selectExpandType != SelectExpandType.Disabled;
            }
            else
            {
                return this.DefaultSelectType.HasValue && this.DefaultSelectType != SelectExpandType.Disabled;
            }
        }

        internal bool Sortable(string propertyName)
        {
            bool enable;
            if (this.OrderByConfigurations.TryGetValue(propertyName, out enable))
            {
                return enable;
            }
            else
            {
                return this.DefaultEnableOrderBy == true;
            }
        }

        internal bool Filterable(string propertyName)
        {
            bool enable;
            if (this.FilterConfigurations.TryGetValue(propertyName, out enable))
            {
                return enable;
            }
            else
            {
                return this.DefaultEnableFilter == true;
            }
        }

        internal bool IsAutomaticExpand(string propertyName)
        {
            ExpandConfiguration expandConfiguration;
            if (this.ExpandConfigurations.TryGetValue(propertyName, out expandConfiguration))
            {
                return expandConfiguration.ExpandType == SelectExpandType.Automatic;
            }
            else
            {
                return this.DefaultExpandType.HasValue && this.DefaultExpandType == SelectExpandType.Automatic;
            }
        }
        
        internal bool IsAutomaticSelect(string propertyName)
        {
            SelectExpandType selectExpandType;
            if (this.SelectConfigurations.TryGetValue(propertyName, out selectExpandType))
            {
                return selectExpandType == SelectExpandType.Automatic;
            }
            else
            {
                return this.DefaultSelectType.HasValue && this.DefaultSelectType == SelectExpandType.Automatic;
            }
        }
    }
}
