namespace Community.OData.Linq.OData.Query
{
    using System.Linq;

    using Community.OData.Linq.Common;
    using Community.OData.Linq.Properties;

    using Microsoft.OData;

    public class TopSkipHelper
    {
        public static IQueryable<T> ApplyTopWithValidation<T>(IQueryable<T> query, long? top, ODataSettings settings)
        {
            if (top.HasValue)
            {
                if (top.Value > int.MaxValue)
                {
                    throw new ODataException(
                        Error.Format(
                            SRResources.SkipTopLimitExceeded,
                            int.MaxValue,
                            AllowedQueryOptions.Top,
                            top.Value));
                }

                if (top.Value > settings.ValidationSettings.MaxTop)
                {
                    throw new ODataException(
                        Error.Format(
                            SRResources.SkipTopLimitExceeded,
                            settings.ValidationSettings.MaxTop,
                            AllowedQueryOptions.Top,
                            top.Value));
                }

                IQueryable<T> result = ExpressionHelpers.Take(
                                           query,
                                           (int)top.Value,
                                           typeof(T),
                                           settings.QuerySettings.EnableConstantParameterization) as IQueryable<T>;

                return result;
            }

            return query;
        }

        public static IQueryable<T> ApplySkipWithValidation<T>(IQueryable<T> query, long? skip, ODataSettings settings)
        {
            if (skip.HasValue)
            {
                if (skip.Value > int.MaxValue)
                {
                    throw new ODataException(
                        Error.Format(
                            SRResources.SkipTopLimitExceeded,
                            int.MaxValue,
                            AllowedQueryOptions.Skip,
                            skip.Value));
                }

                if (skip.Value > settings.ValidationSettings.MaxSkip)
                {
                    throw new ODataException(
                        Error.Format(
                            SRResources.SkipTopLimitExceeded,
                            settings.ValidationSettings.MaxSkip,
                            AllowedQueryOptions.Skip,
                            skip.Value));
                }

                IQueryable<T> result = ExpressionHelpers.Skip(
                    query,
                    (int)skip.Value,
                    typeof(T),
                    settings.QuerySettings.EnableConstantParameterization) as IQueryable<T>;

                return result;
            }

            return query;
        }
    }
}