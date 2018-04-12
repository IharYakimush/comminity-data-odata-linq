// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Community.OData.Linq.OData.Query
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.Serialization;

    /// <summary>
    /// Represents the raw query values in the string format from the incoming request.
    /// </summary>
    [DataContract]
    [Serializable]
    public class ODataRawQueryOptions : IODataQueryOptions
    {
        /// <summary>
        ///  Gets the raw $filter query value from the incoming request Uri if exists.
        /// </summary>
        [IgnoreDataMember]
        IReadOnlyCollection<string> IODataQueryOptions.Filters
        {
            get
            {
                if (this.Filter == null)
                {
                    return Enumerable.Empty<string>().ToArray();
                }

                return Enumerable.Repeat(this.Filter, 1).ToArray();
            }
        }

        [DataMember(Name = "$filter")]
        public virtual string Filter { get; set; }

        /// <summary>
        ///  Gets the raw $apply query value from the incoming request Uri if exists.
        /// </summary>
        //public string Apply { get; set; }

        /// <summary>
        ///  Gets the raw $orderby query value from the incoming request Uri if exists.
        /// </summary>
        [DataMember(Name = "$orderby")]
        public string OrderBy { get; set; }

        /// <summary>
        ///  Gets the raw $top query value from the incoming request Uri if exists.
        /// </summary>
        [DataMember(Name = "$top")]
        public string Top { get; set; }

        /// <summary>
        ///  Gets the raw $skip query value from the incoming request Uri if exists.
        /// </summary>
        [DataMember(Name = "$skip")]
        public string Skip { get; set; }

        /// <summary>
        ///  Gets the raw $select query value from the incoming request Uri if exists.
        /// </summary>
        [DataMember(Name = "$select")]
        public string Select { get; set; }

        /// <summary>
        ///  Gets the raw $expand query value from the incoming request Uri if exists.
        /// </summary>
        [DataMember(Name = "$expand")]
        public string Expand { get; set; }

        /// <summary>
        ///  Gets the raw $count query value from the incoming request Uri if exists.
        /// </summary>
        //public string Count { get; set; }

        /// <summary>
        ///  Gets the raw $format query value from the incoming request Uri if exists.
        /// </summary>
        //public string Format { get; set; }

        /// <summary>
        ///  Gets the raw $skiptoken query value from the incoming request Uri if exists.
        /// </summary>
        //public string SkipToken { get; set; }

        /// <summary>
        ///  Gets the raw $deltatoken query value from the incoming request Uri if exists.
        /// </summary>
        //public string DeltaToken { get; set; }
    }
}
