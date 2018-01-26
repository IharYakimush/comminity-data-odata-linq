// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Community.OData.Linq.OData.Query
{
    using System.Collections.Generic;
    using System.Linq;

    using Community.OData.Linq.Common;

    /// <summary>
    /// Represents a class that truncates a collection to a given page size.
    /// </summary>
    /// <typeparam name="T">The collection element type.</typeparam>
    public class TruncatedCollection<T> : List<T>, ITruncatedCollection, IEnumerable<T>, ICountOptionCollection
    {
        private const int MinPageSize = 1;

        private bool _isTruncated;
        private int _pageSize;
        private long? _totalCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="TruncatedCollection{T}"/> class.
        /// </summary>
        /// <param name="source">The collection to be truncated.</param>
        /// <param name="pageSize">The page size.</param>
        public TruncatedCollection(IEnumerable<T> source, int pageSize)
            : base(source.Take(checked(pageSize + 1)))
        {
            this.Initialize(pageSize);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TruncatedCollection{T}"/> class.
        /// </summary>
        /// <param name="source">The queryable collection to be truncated.</param>
        /// <param name="pageSize">The page size.</param>
        // NOTE: The queryable version calls Queryable.Take which actually gets translated to the backend query where as 
        // the enumerable version just enumerates and is inefficient.
        public TruncatedCollection(IQueryable<T> source, int pageSize)
            : base(source.Take(checked(pageSize + 1)))
        {
            this.Initialize(pageSize);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TruncatedCollection{T}"/> class.
        /// </summary>
        /// <param name="source">The queryable collection to be truncated.</param>
        /// <param name="pageSize">The page size.</param>
        /// <param name="totalCount">The total count.</param>
        public TruncatedCollection(IEnumerable<T> source, int pageSize, long? totalCount)
            : base(pageSize > 0 ? source.Take(checked(pageSize + 1)) : source)
        {
            if (pageSize > 0)
            {
                this.Initialize(pageSize);
            }

            this._totalCount = totalCount;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TruncatedCollection{T}"/> class.
        /// </summary>
        /// <param name="source">The queryable collection to be truncated.</param>
        /// <param name="pageSize">The page size.</param>
        /// <param name="totalCount">The total count.</param>
        // NOTE: The queryable version calls Queryable.Take which actually gets translated to the backend query where as 
        // the enumerable version just enumerates and is inefficient.
        public TruncatedCollection(IQueryable<T> source, int pageSize, long? totalCount)
            : base(pageSize > 0 ? source.Take(checked(pageSize + 1)) : source)
        {
            if (pageSize > 0)
            {
                this.Initialize(pageSize);
            }

            this._totalCount = totalCount;
        }

        private void Initialize(int pageSize)
        {
            if (pageSize < MinPageSize)
            {
                throw Error.ArgumentMustBeGreaterThanOrEqualTo("pageSize", pageSize, MinPageSize);
            }

            this._pageSize = pageSize;

            if (this.Count > pageSize)
            {
                this._isTruncated = true;
                this.RemoveAt(this.Count - 1);
            }
        }

        /// <inheritdoc />
        public int PageSize
        {
            get { return this._pageSize; }
        }

        /// <inheritdoc />
        public bool IsTruncated
        {
            get { return this._isTruncated; }
        }

        /// <inheritdoc />
        public long? TotalCount
        {
            get { return this._totalCount; }
        }
    }
}
