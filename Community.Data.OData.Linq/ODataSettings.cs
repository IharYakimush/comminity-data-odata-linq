namespace Community.OData.Linq
{
    using System.ComponentModel.Design;

    using Community.OData.Linq.OData.Query;

    using Microsoft.OData.UriParser;

    public class ODataSettings
    {
        internal static ODataUriResolver DefaultResolver = new StringAsEnumResolver { EnableCaseInsensitive = true };

        public ODataQuerySettings QuerySettings { get; } = new ODataQuerySettings();

        public ODataValidationSettings ValidationSettings { get; } = new ODataValidationSettings();

        public ODataUriParserSettings ParserSettings { get; } = new ODataUriParserSettings();

        public ODataUriResolver Resolver { get; set; } = DefaultResolver;

        public bool EnableCaseInsensitive { get; set; } = true;
    }
}