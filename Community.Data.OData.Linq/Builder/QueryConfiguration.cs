// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Community.OData.Linq.Builder
{
    using System.Collections.Generic;

    using Community.OData.Linq.OData.Query;

    /// <summary>
    /// Query configuration which contains <see cref="ModelBoundQuerySettings"/>.
    /// </summary>
    public class QueryConfiguration
    {
        private ModelBoundQuerySettings _querySettings;

        /// <summary>
        /// Gets or sets the <see cref="ModelBoundQuerySettings"/>.
        /// </summary>
        public ModelBoundQuerySettings ModelBoundQuerySettings
        {
            get
            {
                return this._querySettings;
            }
            set
            {
                this._querySettings = value;
            }
        }

        /// <summary>
        /// Sets the Countable in <see cref="ModelBoundQuerySettings"/>.
        /// </summary>
        public virtual void SetCount(bool enableCount)
        {
            this.GetModelBoundQuerySettingsOrDefault().Countable = enableCount;
        }

        /// <summary>
        /// Sets the MaxTop in <see cref="ModelBoundQuerySettings"/>.
        /// </summary>
        public virtual void SetMaxTop(int? maxTop)
        {
            this.GetModelBoundQuerySettingsOrDefault().MaxTop = maxTop;
        }

        /// <summary>
        /// Sets the PageSize in <see cref="ModelBoundQuerySettings"/>.
        /// </summary>
        public virtual void SetPageSize(int? pageSize)
        {
            this.GetModelBoundQuerySettingsOrDefault().PageSize = pageSize;
        }

        /// <summary>
        /// Sets the ExpandConfigurations in <see cref="ModelBoundQuerySettings"/>.
        /// </summary>
        public virtual void SetExpand(IEnumerable<string> properties, int? maxDepth, SelectExpandType expandType)
        {
            this.GetModelBoundQuerySettingsOrDefault();
            if (properties == null)
            {
                this.ModelBoundQuerySettings.DefaultExpandType = expandType;
                this.ModelBoundQuerySettings.DefaultMaxDepth = maxDepth ?? ODataValidationSettings.DefaultMaxExpansionDepth;
            }
            else
            {
                foreach (var property in properties)
                {
                    this.ModelBoundQuerySettings.ExpandConfigurations[property] = new ExpandConfiguration
                    {
                        ExpandType = expandType,
                        MaxDepth = maxDepth ?? ODataValidationSettings.DefaultMaxExpansionDepth
                    };
                }
            }
        }

        /// <summary>
        /// Sets the SelectConfigurations in <see cref="ModelBoundQuerySettings"/>.
        /// </summary>
        public virtual void SetSelect(IEnumerable<string> properties, SelectExpandType selectType)
        {
            this.GetModelBoundQuerySettingsOrDefault();
            if (properties == null)
            {
                this.ModelBoundQuerySettings.DefaultSelectType = selectType;
            }
            else
            {
                foreach (var property in properties)
                {
                    this.ModelBoundQuerySettings.SelectConfigurations[property] = selectType;
                }
            }
        }

        /// <summary>
        /// Sets the OrderByConfigurations in <see cref="ModelBoundQuerySettings"/>.
        /// </summary>
        public virtual void SetOrderBy(IEnumerable<string> properties, bool enableOrderBy)
        {
            this.GetModelBoundQuerySettingsOrDefault();
            if (properties == null)
            {
                this.ModelBoundQuerySettings.DefaultEnableOrderBy = enableOrderBy;
            }
            else
            {
                foreach (var property in properties)
                {
                    this.ModelBoundQuerySettings.OrderByConfigurations[property] = enableOrderBy;
                }
            }
        }

        /// <summary>
        /// Sets the FilterConfigurations in <see cref="ModelBoundQuerySettings"/>.
        /// </summary>
        public virtual void SetFilter(IEnumerable<string> properties, bool enableFilter)
        {
            this.GetModelBoundQuerySettingsOrDefault();
            if (properties == null)
            {
                this.ModelBoundQuerySettings.DefaultEnableFilter = enableFilter;
            }
            else
            {
                foreach (var property in properties)
                {
                    this.ModelBoundQuerySettings.FilterConfigurations[property] = enableFilter;
                }
            }
        }

        /// <summary>
        /// Gets the <see cref="ModelBoundQuerySettings"/> or create it depends on the default settings.
        /// </summary>
        internal ModelBoundQuerySettings GetModelBoundQuerySettingsOrDefault()
        {
            if (this._querySettings == null)
            {
                this._querySettings = new ModelBoundQuerySettings(ModelBoundQuerySettings.DefaultModelBoundQuerySettings);
            }

            return this._querySettings;
        }
    }
}
