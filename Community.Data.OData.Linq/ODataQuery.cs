using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;

namespace Community.Data.OData.Linq
{
    public class ODataQuery<T> : IQueryable<T>, IQueryable
    {
        public ODataQuery(IQueryable inner, IServiceProvider serviceProvider)
        {
            this.Inner = inner ?? throw new ArgumentNullException(nameof(inner));
            ServiceProvider = serviceProvider;
        }

        public IEdmModel EdmModel => this.ServiceProvider.GetRequiredService<IEdmModel>();

        public Type ElementType => Inner.ElementType;

        public Expression Expression => Inner.Expression;

        public IQueryProvider Provider => Inner.Provider;

        public IQueryable Inner { get; }
        public IServiceProvider ServiceProvider { get; }

        public IEnumerator GetEnumerator()
        {
            return Inner.GetEnumerator();
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return (IEnumerator<T>)Inner.GetEnumerator();
        }
    }
}
