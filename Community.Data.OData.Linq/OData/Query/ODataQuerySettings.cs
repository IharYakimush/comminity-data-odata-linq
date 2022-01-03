// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Community.OData.Linq.OData.Query
{
    using Community.OData.Linq.Common;

    using System;

    /// <summary>
    /// This class describes the settings to use during query composition.
    /// </summary>
    public class ODataQuerySettings
    {
        private HandleNullPropagationOption _handleNullPropagationOption = HandleNullPropagationOption.Default;
        private int? _pageSize;
        private int? _modelBoundPageSize;
        private TimeZoneInfo defaultTimeZone;

        /// <summary>
        /// Instantiates a new instance of the <see cref="ODataQuerySettings"/> class
        /// and initializes the default settings.
        /// </summary>
        public ODataQuerySettings()
        {
            this.EnsureStableOrdering = true;
            this.EnableConstantParameterization = true;
        }

        /// <summary>
        /// Gets or sets the maximum number of query results to return based on the type or property.
        /// </summary>
        /// <value>
        /// The maximum number of query results to return based on the type or property,
        /// or <c>null</c> if there is no limit.
        /// </value>
        internal int? ModelBoundPageSize
        {
            get
            {
                return this._modelBoundPageSize;
            }
            set
            {
                if (value.HasValue && value <= 0)
                {
                    throw Error.ArgumentMustBeGreaterThanOrEqualTo("value", value, 1);
                }

                this._modelBoundPageSize = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether query composition should
        /// alter the original query when necessary to ensure a stable sort order.
        /// </summary>
        /// <value>A <c>true</c> value indicates the original query should
        /// be modified when necessary to guarantee a stable sort order.
        /// A <c>false</c> value indicates the sort order can be considered
        /// stable without modifying the query.  Query providers that ensure
        /// a stable sort order should set this value to <c>false</c>.
        /// The default value is <c>true</c>.</value>
        public bool EnsureStableOrdering { get; set; }

        /// <summary>
        /// Gets or sets timezone value which will be used to convert <see cref="DateTimeOffset"/> from filter query to <see cref="DateTime"/> when it is used as property type.
        /// </summary>
        /// <value><list type="">
        /// <item><c>TimeZoneInfo.Local</c> - default value. </item>
        /// <item><c>TimeZoneInfo.Utc</c> - useful when property of type <see cref="DateTimeOffset"/> can't be used in the model and as a workaround we have <see cref="DateTime"/> type and convention to store UTC values.</item>
        /// </list></value>        
        public TimeZoneInfo DefaultTimeZone { get => defaultTimeZone ?? TimeZoneInfo.Local; set => defaultTimeZone = value; }

        /// <summary>
        /// Gets or sets a value indicating how null propagation should
        /// be handled during query composition.
        /// </summary>
        /// <value>
        /// The default is <see cref="HandleNullPropagationOption.Default"/>.
        /// </value>
        public HandleNullPropagationOption HandleNullPropagation
        {
            get
            {
                return this._handleNullPropagationOption;
            }
            set
            {
                HandleNullPropagationOptionHelper.Validate(value, "value");
                this._handleNullPropagationOption = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether constants should be parameterized. Parameterizing constants
        /// would result in better performance with Entity framework.
        /// </summary>
        /// <value>The default value is <c>true</c>.</value>
        public bool EnableConstantParameterization { get; set; }

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

        internal void CopyFrom(ODataQuerySettings settings)
        {
            this.EnsureStableOrdering = settings.EnsureStableOrdering;
            this.EnableConstantParameterization = settings.EnableConstantParameterization;
            this.HandleNullPropagation = settings.HandleNullPropagation;
            this.PageSize = settings.PageSize;
            this.ModelBoundPageSize = settings.ModelBoundPageSize;
            this.defaultTimeZone = settings.defaultTimeZone;
        }

        public override bool Equals(object obj)
        {
            return obj is ODataQuerySettings settings &&
                   _handleNullPropagationOption == settings._handleNullPropagationOption &&
                   _pageSize == settings._pageSize &&
                   _modelBoundPageSize == settings._modelBoundPageSize &&
                   DefaultTimeZone == settings.DefaultTimeZone &&
                   EnsureStableOrdering == settings.EnsureStableOrdering &&
                   EnableConstantParameterization == settings.EnableConstantParameterization;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_handleNullPropagationOption, _pageSize, _modelBoundPageSize, EnsureStableOrdering, EnableConstantParameterization, DefaultTimeZone);
        }
    }
}
