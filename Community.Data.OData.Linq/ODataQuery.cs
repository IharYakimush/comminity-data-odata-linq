namespace Community.OData.Linq
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;

    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.OData.Edm;

    public class ODataQuery<T> : IQueryable<T>, IQueryable
    {
        public ODataQuery(IQueryable inner, IServiceProvider serviceProvider)
        {
            this.Inner = inner ?? throw new ArgumentNullException(nameof(inner));
            this.ServiceProvider = serviceProvider;
        }

        public IEdmModel EdmModel => this.ServiceProvider.GetRequiredService<IEdmModel>();

        public Type ElementType => this.Inner.ElementType;

        public Expression Expression => this.Inner.Expression;

        public IQueryProvider Provider => this.Inner.Provider;

        public IQueryable Inner { get; }
        public IServiceProvider ServiceProvider { get; }

        public IEnumerator GetEnumerator()
        {
            return this.Inner.GetEnumerator();
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return (IEnumerator<T>)this.Inner.GetEnumerator();
        }
    }
}
