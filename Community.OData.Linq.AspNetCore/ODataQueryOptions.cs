namespace Community.OData.Linq.AspNetCore
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.Serialization;

    using Community.OData.Linq.OData.Query;

    using Microsoft.AspNetCore.Mvc;

    [DataContract]
    [Serializable]
    [ModelBinder(BinderType = typeof(ODataQueryOptionsModelBinder))]
    public class ODataQueryOptions : ODataRawQueryOptions, IODataRawQueryOptions
    {
        public IReadOnlyCollection<string> Filters { get; set; }

        public override string Filter
        {
            get
            {
                if (this.Filters != null)
                {
                    if (this.Filters.Count == 1)
                    {
                        return this.Filters.First();
                    }

                    return string.Join(" and ", this.Filters.Select(s => $"({s})"));
                }

                return null;
            }

            set => throw new NotSupportedException(
                       $"Setting {nameof(this.Filter)} property not supported. Use {this.Filters} instead");
        }
    }
}