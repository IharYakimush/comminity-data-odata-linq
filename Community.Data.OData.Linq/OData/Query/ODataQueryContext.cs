// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Community.OData.Linq.OData.Query
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq;

    using Community.OData.Linq.Common;
    using Community.OData.Linq.OData.Formatter;
    using Community.OData.Linq.Properties;

    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.OData.Edm;
    using Microsoft.OData.UriParser;

    /// <summary>
    /// This defines some context information used to perform query composition.
    /// </summary>
    public class ODataQueryContext
    {
        private DefaultQuerySettings _defaultQuerySettings;

        /// <summary>
        /// Constructs an instance of <see cref="ODataQueryContext"/> with <see cref="IEdmModel" />, element CLR type,
        /// and <see cref="ODataPath" />.
        /// </summary>
        /// <param name="model">The EdmModel that includes the <see cref="IEdmType"/> corresponding to
        /// the given <paramref name="elementClrType"/>.</param>
        /// <param name="elementClrType">The CLR type of the element of the collection being queried.</param>
        /// <param name="path">The parsed <see cref="ODataPath"/>.</param>
        public ODataQueryContext(IEdmModel model, Type elementClrType, ODataPath path)
        {
            if (model == null)
            {
                throw Error.ArgumentNull("model");
            }

            if (elementClrType == null)
            {
                throw Error.ArgumentNull("elementClrType");
            }

            this.ElementType = model.GetEdmType(elementClrType);

            if (this.ElementType == null)
            {
                throw Error.Argument("elementClrType", SRResources.ClrTypeNotInModel, elementClrType.FullName);
            }

            this.ElementClrType = elementClrType;
            this.Model = model;
            this.Path = path;
            this.NavigationSource = GetNavigationSource(this.Model, this.ElementType, path);
            this.GetPathContext();
        }

        /// <summary>
        /// Constructs an instance of <see cref="ODataQueryContext"/> with <see cref="IEdmModel" />, element EDM type,
        /// and <see cref="ODataPath" />.
        /// </summary>
        /// <param name="model">The EDM model the given EDM type belongs to.</param>
        /// <param name="elementType">The EDM type of the element of the collection being queried.</param>
        /// <param name="path">The parsed <see cref="ODataPath"/>.</param>
        public ODataQueryContext(IEdmModel model, IEdmType elementType, ODataPath path)
        {
            if (model == null)
            {
                throw Error.ArgumentNull("model");
            }
            if (elementType == null)
            {
                throw Error.ArgumentNull("elementType");
            }

            this.Model = model;
            this.ElementType = elementType;
            this.Path = path;
            this.NavigationSource = GetNavigationSource(this.Model, this.ElementType, path);
            this.GetPathContext();
        }

        internal ODataQueryContext(IEdmModel model, Type elementClrType)
            : this(model, elementClrType, path: null)
        {
        }

        internal ODataQueryContext(IEdmModel model, IEdmType elementType)
            : this(model, elementType, path: null)
        {
        }

        /// <summary>
        /// Gets the given <see cref="DefaultQuerySettings"/>.
        /// </summary>
        public DefaultQuerySettings DefaultQuerySettings
        {
            get
            {
                if (this._defaultQuerySettings == null)
                {
                    this._defaultQuerySettings = this.RequestContainer != null
                        ? this.RequestContainer.GetRequiredService<DefaultQuerySettings>()
                        : new DefaultQuerySettings();
                }

                return this._defaultQuerySettings;
            }
        }

        /// <summary>
        /// Gets the given <see cref="IEdmModel"/> that contains the EntitySet.
        /// </summary>
        public IEdmModel Model { get; private set; }

        /// <summary>
        /// Gets the <see cref="IEdmType"/> of the element.
        /// </summary>
        public IEdmType ElementType { get; private set; }

        /// <summary>
        /// Gets the <see cref="IEdmNavigationSource"/> that contains the element.
        /// </summary>
        public IEdmNavigationSource NavigationSource { get; private set; }

        /// <summary>
        /// Gets the CLR type of the element.
        /// </summary>
        public Type ElementClrType { get; internal set; }

        /// <summary>
        /// Gets the <see cref="ODataPath"/>.
        /// </summary>
        public ODataPath Path { get; private set; }

        /// <summary>
        /// Gets the request container.
        /// </summary>
        public IServiceProvider RequestContainer { get; internal set; }

        internal IEdmProperty TargetProperty { get; private set; }

        internal IEdmStructuredType TargetStructuredType { get; private set; }

        internal string TargetName { get; private set; }

        private static IEdmNavigationSource GetNavigationSource(IEdmModel model, IEdmType elementType, ODataPath odataPath)
        {
            Contract.Assert(model != null);
            Contract.Assert(elementType != null);
            
            IEdmEntityContainer entityContainer = model.EntityContainer;
            if (entityContainer == null)
            {
                return null;
            }

            List<IEdmEntitySet> matchedNavigationSources =
                entityContainer.EntitySets().Where(e => e.EntityType() == elementType).ToList();

            return (matchedNavigationSources.Count != 1) ? null : matchedNavigationSources[0];
        }

        private void GetPathContext()
        {
            if (this.Path != null)
            {
                IEdmProperty property;
                IEdmStructuredType structuredType;
                string name;
                EdmLibHelpers.GetPropertyAndStructuredTypeFromPath(
                    this.Path,
                    out property,
                    out structuredType,
                    out name);

                this.TargetProperty = property;
                this.TargetStructuredType = structuredType;
                this.TargetName = name;
            }
            else
            {
                this.TargetStructuredType = this.ElementType as IEdmStructuredType;
            }
        }
    }
}
