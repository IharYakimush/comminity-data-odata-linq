// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Community.Data.OData.Linq.Builder
{
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq;

    using Microsoft.OData.Edm;
    using Microsoft.OData.UriParser;

    internal class ContainmentPathBuilder
    {
        private List<ODataPathSegment> _segments;

       

        private void RemovePathSegmentsAfterTheLastNavigationProperty()
        {
            // Find the last navigation property segment.
            ODataPathSegment lastNavigationProperty = this._segments.OfType<NavigationPropertySegment>().LastOrDefault();
            List<ODataPathSegment> newSegments = new List<ODataPathSegment>();
            foreach (ODataPathSegment segment in this._segments)
            {
                newSegments.Add(segment);
                if (segment == lastNavigationProperty)
                {
                    break;
                }
            }

            this._segments = newSegments;
        }

        private void RemoveRedundantContainingPathSegments()
        {
            // Find the last non-contained navigation property segment:
            //   Collection valued: entity set
            //   -or-
            //   Single valued: singleton
            // Copy over other path segments such as: not a navigation path segment, contained navigation property,
            // single valued navigation property with navigation source targetting an entity set (we won't have key
            // information for that navigation property.)
            this._segments.Reverse();
            NavigationPropertySegment navigationPropertySegment = null;
            List<ODataPathSegment> newSegments = new List<ODataPathSegment>();
            foreach (ODataPathSegment segment in this._segments)
            {
                navigationPropertySegment = segment as NavigationPropertySegment;
                if (navigationPropertySegment != null)
                {
                    EdmNavigationSourceKind navigationSourceKind =
                        navigationPropertySegment.NavigationSource.NavigationSourceKind();
                    if ((navigationPropertySegment.NavigationProperty.TargetMultiplicity() == EdmMultiplicity.Many &&
                         navigationSourceKind == EdmNavigationSourceKind.EntitySet) ||
                        (navigationSourceKind == EdmNavigationSourceKind.Singleton))
                    {
                        break;
                    }
                }

                newSegments.Insert(0, segment);
            }

            // Start the path with the navigation source of the navigation property found above.
            if (navigationPropertySegment != null)
            {
                IEdmNavigationSource navigationSource = navigationPropertySegment.NavigationSource;
                Contract.Assert(navigationSource != null);
                if (navigationSource.NavigationSourceKind() == EdmNavigationSourceKind.Singleton)
                {
                    SingletonSegment singletonSegment = new SingletonSegment((IEdmSingleton)navigationSource);
                    newSegments.Insert(0, singletonSegment);
                }
                else
                {
                    Contract.Assert(navigationSource.NavigationSourceKind() == EdmNavigationSourceKind.EntitySet);
                    EntitySetSegment entitySetSegment = new EntitySetSegment((IEdmEntitySet)navigationSource);
                    newSegments.Insert(0, entitySetSegment);
                }
            }

            this._segments = newSegments;
        }

        private void RemoveAllTypeCasts()
        {
            List<ODataPathSegment> newSegments = new List<ODataPathSegment>();
            foreach (ODataPathSegment segment in this._segments)
            {
                if (!(segment is TypeSegment))
                {
                    newSegments.Add(segment);
                }
            }

            this._segments = newSegments;
        }

        private void AddTypeCastsIfNecessary()
        {
            IEdmEntityType owningType = null;
            List<ODataPathSegment> newSegments = new List<ODataPathSegment>();
            foreach (ODataPathSegment segment in this._segments)
            {
                NavigationPropertySegment navProp = segment as NavigationPropertySegment;
                if (navProp != null && owningType != null &&
                    owningType.FindProperty(navProp.NavigationProperty.Name) == null)
                {
                    // need a type cast
                    TypeSegment typeCast = new TypeSegment(
                        navProp.NavigationProperty.DeclaringType,
                        navigationSource: null);
                    newSegments.Add(typeCast);
                }

                newSegments.Add(segment);
                IEdmEntityType targetEntityType = GetTargetEntityType(segment);
                if (targetEntityType != null)
                {
                    owningType = targetEntityType;
                }
            }

            this._segments = newSegments;
        }

        private static IEdmEntityType GetTargetEntityType(ODataPathSegment segment)
        {
            Contract.Assert(segment != null);

            EntitySetSegment entitySetSegment = segment as EntitySetSegment;
            if (entitySetSegment != null)
            {
                return entitySetSegment.EntitySet.EntityType();
            }

            SingletonSegment singletonSegment = segment as SingletonSegment;
            if (singletonSegment != null)
            {
                return singletonSegment.Singleton.EntityType();
            }

            NavigationPropertySegment navigationPropertySegment = segment as NavigationPropertySegment;
            if (navigationPropertySegment != null)
            {
                return navigationPropertySegment.NavigationSource.EntityType();
            }

            return null;
        }
    }
}
