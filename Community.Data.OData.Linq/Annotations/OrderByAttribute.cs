// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Community.Data.OData.Linq.Annotations
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;

    /// <summary>
    /// Represents an <see cref="Attribute"/> that can be placed on a class or property
    /// correlate to OData's $orderby query option settings.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = true)]
    [SuppressMessage("Microsoft.Design", "CA1019:DefineAccessorsForAttributeArguments",
        Justification = "Don't want those argument to be retrievable")]
    public sealed class OrderByAttribute : Attribute
    {
        private bool? _defaultEnableOrderBy;
        private bool _disable;
        private readonly Dictionary<string, bool> _orderByConfigurations = new Dictionary<string, bool>();
        
        /// <summary>
        /// Initializes a new instance of the <see cref="OrderByAttribute"/> class.
        /// </summary>
        public OrderByAttribute()
        {
            this._defaultEnableOrderBy = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderByAttribute"/> class
        /// with the name of allowed $orderby properties.
        /// </summary>
        public OrderByAttribute(params string[] properties)
        {
            foreach (var property in properties)
            {
                if (!this._orderByConfigurations.ContainsKey(property))
                {
                    this._orderByConfigurations.Add(property, true);
                }
            }
        }

        /// <summary>
        /// Gets or sets the $orderby configuration of properties.
        /// </summary>
        public Dictionary<string, bool> OrderByConfigurations
        {
            get
            {
                return this._orderByConfigurations;
            }
        }

        /// <summary>
        /// Represents whether the $orderby can be applied on those properties.
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
                List<string> keys = this._orderByConfigurations.Keys.ToList();
                foreach (var property in keys)
                {
                    this._orderByConfigurations[property] = !this._disable;
                }

                if (this._orderByConfigurations.Count == 0)
                {
                    this._defaultEnableOrderBy = !this._disable;
                }
            }
        }

        internal bool? DefaultEnableOrderBy
        {
            get
            {
                return this._defaultEnableOrderBy;
            }
            set
            {
                this._defaultEnableOrderBy = value;
            }
        }
    }
}
