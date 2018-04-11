namespace Community.OData.Linq
{
    public class ODataParameters
    {
        /// <summary>
        ///     Gets or sets the raw $apply query value from the incoming request Uri if exists.
        /// </summary>
        public string Apply { get; set; }

        /// <summary>
        ///     Gets or sets the raw $count query value from the incoming request Uri if exists.
        /// </summary>
        public string Count { get; set; }

        /// <summary>
        ///     Gets or sets the raw $deltatoken query value from the incoming request Uri if exists.
        /// </summary>
        public string DeltaToken { get; set; }

        /// <summary>
        ///     Gets or sets the raw $expand query value from the incoming request Uri if exists.
        /// </summary>
        public string Expand { get; set; }

        /// <summary>
        ///     Gets or sets the raw $filter query value from the incoming request Uri if exists.
        /// </summary>
        public string Filter { get; set; }

        /// <summary>
        ///     Gets or sets the raw $format query value from the incoming request Uri if exists.
        /// </summary>
        public string Format { get; set; }

        /// <summary>
        ///     Gets or sets the raw $orderby query value from the incoming request Uri if exists.
        /// </summary>
        public string OrderBy { get; set; }

        /// <summary>
        ///     Gets or sets the raw $select query value from the incoming request Uri if exists.
        /// </summary>
        public string Select { get; set; }

        /// <summary>
        ///     Gets or sets the raw $skip query value from the incoming request Uri if exists.
        /// </summary>
        public string Skip { get; set; }

        /// <summary>
        ///     Gets or sets the raw $skiptoken query value from the incoming request Uri if exists.
        /// </summary>
        public string SkipToken { get; set; }

        /// <summary>
        ///     Gets or sets the raw $top query value from the incoming request Uri if exists.
        /// </summary>
        public string Top { get; set; }
    }
}