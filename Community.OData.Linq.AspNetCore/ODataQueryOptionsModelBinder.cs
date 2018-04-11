namespace Community.OData.Linq.AspNetCore
{
    using System;
    using System.Threading.Tasks;

    using Microsoft.AspNetCore.Mvc.ModelBinding;

    public class ODataQueryOptionsModelBinder : IModelBinder
    {        
        public async Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
            {
                throw new ArgumentNullException(nameof(bindingContext));
            }

            ODataQueryOptions result = new ODataQueryOptions();

            ValueProviderResult filter = bindingContext.ValueProvider.GetValue("$filter");
            if (filter.Values.Count > 0)
            {
                result.Filters = filter.Values.ToArray();
            }

            result.OrderBy = EnsureSingleParam(bindingContext, "$orderby");
            result.Top = EnsureSingleParam(bindingContext, "$top");
            result.Skip = EnsureSingleParam(bindingContext, "$skip");
            result.Select = EnsureSingleParam(bindingContext, "$select");
            result.Expand = EnsureSingleParam(bindingContext, "$expand");

            if (bindingContext.ModelState.IsValid)
            {
                bindingContext.Result = ModelBindingResult.Success(result);
            }
            else
            {
                bindingContext.Result = ModelBindingResult.Failed();
            }
        }

        private static string EnsureSingleParam(ModelBindingContext bindingContext, string key)
        {
            var valueProvider = bindingContext.ValueProvider.GetValue(key);
            if (valueProvider.Values.Count > 0)
            {
                if (valueProvider.Values.Count > 1)
                {
                    bindingContext.ModelState.TryAddModelError(key, $"Multiple {key} values provided");
                }

                return valueProvider.FirstValue;
            }

            return null;
        }
    }
}