namespace Community.OData.Linq.AspNetCore
{
    using System;

    using Microsoft.AspNetCore.Mvc.ModelBinding;
    using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

    public class ODataQueryOptionsModeBinderProvider : IModelBinderProvider
    {
        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.Metadata.ModelType == typeof(ODataQueryOptions))
            {
                return new BinderTypeModelBinder(typeof(ODataQueryOptionsModelBinder));
            }

            return null;
        }
    }
}