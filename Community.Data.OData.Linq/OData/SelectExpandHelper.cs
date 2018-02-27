namespace Community.OData.Linq.OData
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    using Community.OData.Linq.OData.Formatter;
    using Community.OData.Linq.OData.Query;

    using Microsoft.OData.Edm;
    using Microsoft.OData.UriParser;

    public class SelectExpandHelper<T>
    {
        private readonly ODataQuery<T> query;

        private readonly string entitySetName;

        private ODataRawQueryOptions RawValues { get; }
        private ODataQueryContext Context { get; }

        public SelectExpandHelper(ODataRawQueryOptions rawQueryOptions, ODataQuery<T> query, string entitySetName)
        {
            this.Context = new ODataQueryContext(query.EdmModel, query.ElementType);
            this.query = query;
            this.entitySetName = entitySetName;
            this.RawValues = rawQueryOptions ?? throw new ArgumentNullException(nameof(rawQueryOptions));
        }
        public void AddAutoSelectExpandProperties()
        {
            bool containsAutoSelectExpandProperties = false;
            var autoExpandRawValue = GetAutoExpandRawValue();
            var autoSelectRawValue = GetAutoSelectRawValue();

            IDictionary<string, string> queryParameters = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(autoExpandRawValue) && !autoExpandRawValue.Equals(RawValues.Expand))
            {
                queryParameters["$expand"] = autoExpandRawValue;
                containsAutoSelectExpandProperties = true;
            }
            else
            {
                autoExpandRawValue = RawValues.Expand;
            }

            if (!string.IsNullOrEmpty(autoSelectRawValue) && !autoSelectRawValue.Equals(RawValues.Select))
            {
                queryParameters["$select"] = autoSelectRawValue;
                containsAutoSelectExpandProperties = true;
            }
            else
            {
                autoSelectRawValue = RawValues.Select;
            }

            if (containsAutoSelectExpandProperties)
            {
                ODataQueryOptionParser parser = ODataLinqExtensions.GetParser(
                    this.query,
                    this.entitySetName,
                    queryParameters);
                //var originalSelectExpand = SelectExpand;
                //SelectExpand = new SelectExpandQueryOption(
                //    autoSelectRawValue,
                //    autoExpandRawValue,
                //    Context,
                //    _queryOptionParser);
                //if (originalSelectExpand != null && originalSelectExpand.LevelsMaxLiteralExpansionDepth > 0)
                //{
                //    SelectExpand.LevelsMaxLiteralExpansionDepth = originalSelectExpand.LevelsMaxLiteralExpansionDepth;
                //}
            }
        }

        private string GetAutoSelectRawValue()
        {
            var selectRawValue = RawValues.Select;
            var autoSelectRawValue = string.Empty;
            IEdmEntityType baseEntityType = Context.TargetStructuredType as IEdmEntityType;
            if (string.IsNullOrEmpty(selectRawValue))
            {
                var autoSelectProperties = EdmLibHelpers.GetAutoSelectProperties(Context.TargetProperty,
                    Context.TargetStructuredType, Context.Model);

                foreach (var property in autoSelectProperties)
                {
                    if (!string.IsNullOrEmpty(autoSelectRawValue))
                    {
                        autoSelectRawValue += ",";
                    }

                    if (baseEntityType != null && property.DeclaringType != baseEntityType)
                    {
                        autoSelectRawValue += string.Format(CultureInfo.InvariantCulture, "{0}/",
                            property.DeclaringType.FullTypeName());
                    }

                    autoSelectRawValue += property.Name;
                }

                if (!string.IsNullOrEmpty(autoSelectRawValue))
                {
                    if (!string.IsNullOrEmpty(selectRawValue))
                    {
                        selectRawValue = string.Format(CultureInfo.InvariantCulture, "{0},{1}",
                            autoSelectRawValue, selectRawValue);
                    }
                    else
                    {
                        selectRawValue = autoSelectRawValue;
                    }
                }
            }

            return selectRawValue;
        }

        private string GetAutoExpandRawValue()
        {
            var expandRawValue = RawValues.Expand;
            IEdmEntityType baseEntityType = Context.TargetStructuredType as IEdmEntityType;
            var autoExpandRawValue = string.Empty;
            var autoExpandNavigationProperties = EdmLibHelpers.GetAutoExpandNavigationProperties(
                Context.TargetProperty, Context.TargetStructuredType, Context.Model,
                !string.IsNullOrEmpty(RawValues.Select));

            foreach (var property in autoExpandNavigationProperties)
            {
                if (!string.IsNullOrEmpty(autoExpandRawValue))
                {
                    autoExpandRawValue += ",";
                }

                if (property.DeclaringEntityType() != baseEntityType)
                {
                    autoExpandRawValue += string.Format(
                        CultureInfo.InvariantCulture,
                        "{0}/",
                        (object)property.DeclaringEntityType().FullTypeName());
                }

                autoExpandRawValue += property.Name;
            }

            if (!string.IsNullOrEmpty(autoExpandRawValue))
            {
                if (!string.IsNullOrEmpty(expandRawValue))
                {
                    expandRawValue = string.Format(CultureInfo.InvariantCulture, "{0},{1}",
                        autoExpandRawValue, expandRawValue);
                }
                else
                {
                    expandRawValue = autoExpandRawValue;
                }
            }
            return expandRawValue;
        }
    }
}