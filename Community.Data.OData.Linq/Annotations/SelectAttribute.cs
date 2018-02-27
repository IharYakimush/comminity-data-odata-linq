// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Community.OData.Linq.Annotations
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;

    using Community.OData.Linq.OData.Query;

    /// <summary>
    /// Represents an <see cref="Attribute"/> that can be placed on a property or a class
    /// correlate to OData's $select query option settings.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, AllowMultiple = true)]
    [SuppressMessage("Microsoft.Design", "CA1019:DefineAccessorsForAttributeArguments",
        Justification = "Don't want those argument to be retrievable")]
    public sealed class SelectAttribute : Attribute
    {
        private readonly Dictionary<string, SelectExpandType> _selectConfigurations = new Dictionary<string, SelectExpandType>();
        private SelectExpandType _selectType;
        private SelectExpandType? _defaultSelectType;

        /// <summary>
        /// Initializes a new instance of the <see cref="SelectAttribute"/> class.
        /// </summary>
        public SelectAttribute()
        {
            this._defaultSelectType = SelectExpandType.Allowed;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SelectAttribute"/> class
        /// with the name of allowed select properties.
        /// </summary>
        public SelectAttribute(params string[] properties)
        {
            foreach (var property in properties)
            {
                if (!this._selectConfigurations.ContainsKey(property))
                {
                    this._selectConfigurations.Add(property, SelectExpandType.Allowed);
                }
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="SelectExpandType"/> of properties.
        /// </summary>
        public Dictionary<string, SelectExpandType> SelectConfigurations
        {
            get
            {
                return this._selectConfigurations;
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="SelectExpandType"/> of properties.
        /// </summary>
        public SelectExpandType SelectType
        {
            get
            {
                return this._selectType;
            }
            set
            {
                this._selectType = value;
                List<string> keys = this._selectConfigurations.Keys.ToList();
                foreach (var property in keys)
                {
                    this._selectConfigurations[property] = this._selectType;
                }

                if (this._selectConfigurations.Count == 0)
                {
                    this._defaultSelectType = this._selectType;
                }
            }
        }

        internal SelectExpandType? DefaultSelectType
        {
            get
            {
                return this._defaultSelectType;
            }
            set
            {
                this._defaultSelectType = value;
            }
        }
    }
}
