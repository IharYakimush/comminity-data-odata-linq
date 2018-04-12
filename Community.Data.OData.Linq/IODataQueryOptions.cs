namespace Community.OData.Linq
{
    using System.Collections.Generic;
    using System.Linq;

    public interface IODataQueryOptions
    {
        /// <summary>
        ///  Gets the raw $filter query value from the incoming request Uri if exists.
        /// </summary>
        IReadOnlyCollection<string> Filters { get; }

        /// <summary>
        ///  Gets the raw $orderby query value from the incoming request Uri if exists.
        /// </summary>
        string OrderBy { get; }

        /// <summary>
        ///  Gets the raw $top query value from the incoming request Uri if exists.
        /// </summary>
        string Top { get; }

        /// <summary>
        ///  Gets the raw $skip query value from the incoming request Uri if exists.
        /// </summary>
        string Skip { get; }

        /// <summary>
        ///  Gets the raw $select query value from the incoming request Uri if exists.
        /// </summary>
        string Select { get; }

        /// <summary>
        ///  Gets the raw $expand query value from the incoming request Uri if exists.
        /// </summary>
        string Expand { get; }
    }
}