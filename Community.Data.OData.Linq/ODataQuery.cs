using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Microsoft.OData.Edm;

namespace Community.Data.OData.Linq
{
    public class ODataQuery<T> : IQueryable<T>, IQueryable
    {
        public ODataQuery(IQueryable inner, IEdmModel edmModel)
        {
            this.EdmModel = edmModel ?? throw new ArgumentNullException(nameof(edmModel));
            this.Inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public IEdmModel EdmModel { get; }
        public Type ElementType => Inner.ElementType;

        public Expression Expression => Inner.Expression;

        public IQueryProvider Provider => throw new NotImplementedException();

        public IQueryable Inner { get; }

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
