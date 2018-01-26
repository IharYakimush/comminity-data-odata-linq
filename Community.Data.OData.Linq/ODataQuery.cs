namespace Community.OData.Linq
{
    using System;
    using System.Linq;

    public class ODataQueryOrdered<T> : ODataQuery<T>
    {
        public ODataQueryOrdered(IOrderedQueryable inner, IServiceProvider serviceProvider):base(inner,serviceProvider)
        {
        }        
    }
}
