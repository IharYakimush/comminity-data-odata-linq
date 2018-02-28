namespace Community.OData.Linq
{
    using Community.OData.Linq.OData.Query;

    using Microsoft.OData.UriParser;

    public class ODataSettings
    {
        internal static ODataUriResolver DefaultResolver = new StringAsEnumResolver { EnableCaseInsensitive = true };

        public ODataQuerySettings QuerySettings { get; } = new ODataQuerySettings() { PageSize = 20 };

        public ODataValidationSettings ValidationSettings { get; } = new ODataValidationSettings();

        public ODataUriParserSettings ParserSettings { get; } = new ODataUriParserSettings();

        public ODataUriResolver Resolver { get; set; } = DefaultResolver;

        public bool EnableCaseInsensitive { get; set; } = true;

        public DefaultQuerySettings DefaultQuerySettings { get; } =
            new DefaultQuerySettings
            {
                EnableFilter = true,
                EnableOrderBy = true,
                EnableExpand = true,
                EnableSelect = true,
                MaxTop = 100
            };
    }
}