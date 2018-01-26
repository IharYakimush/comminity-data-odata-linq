﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Community.Data.OData.Linq.Annotations
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;

    /// <summary>
    /// Represents an <see cref="Attribute"/> that can be placed on a class or property
    /// correlate to OData's $filter query option settings.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = true)]
    [SuppressMessage("Microsoft.Design", "CA1019:DefineAccessorsForAttributeArguments",
        Justification = "Don't want those argument to be retrievable")]
    public sealed class FilterAttribute : Attribute
    {
        private bool? _defaultEnableFilter;
        private bool _disable;
        private readonly Dictionary<string, bool> _filterConfigurations = new Dictionary<string, bool>();

        /// <summary>
        /// Initializes a new instance of the <see cref="FilterAttribute"/> class.
        /// </summary>
        public FilterAttribute()
        {
            this._defaultEnableFilter = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FilterAttribute"/> class
        /// with the name of allowed $filter properties.
        /// </summary>
        public FilterAttribute(params string[] properties)
        {
            foreach (var property in properties)
            {
                this._filterConfigurations.Add(property, true);
            }
        }

        /// <summary>
        /// Gets or sets the $filter configuration of properties.
        /// </summary>
        public Dictionary<string, bool> FilterConfigurations
        {
            get
            {
                return this._filterConfigurations;
            }
        }

        /// <summary>
        /// Represents whether the $filter can be applied on those properties.
        /// </summary>
        public bool Disabled
        {
            get
            {
                return this._disable;
            }
            set
            {
                this._disable = value;
                List<string> keys = this._filterConfigurations.Keys.ToList();
                foreach (var property in keys)
                {
                    this._filterConfigurations[property] = !this._disable;
                }

                if (this._filterConfigurations.Count == 0)
                {
                    this._defaultEnableFilter = !this._disable;
                }
            }
        }

        internal bool? DefaultEnableFilter
        {
            get
            {
                return this._defaultEnableFilter;
            }
            set
            {
                this._defaultEnableFilter = value;
            }
        }
    }
}