using BenchmarkDotNet.Attributes;

using Community.OData.Linq;
using Community.OData.Linq.Builder;
using Community.OData.Linq.OData.Query;
using Community.OData.Linq.OData.Query.Expressions;
using Community.OData.Linq.xTests.SampleData;

using Microsoft.OData;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;

namespace Benchmark
{
    public class InitQuery
    {
        private static readonly ODataSimplifiedOptions SimplifiedOptions = new ODataSimplifiedOptions();

        private static ODataUriResolver DefaultResolver = new StringAsEnumResolver { EnableCaseInsensitive = true };

        private static readonly IEdmModel defaultEdmModel;

        private static readonly IQueryable<ClassWithDeepNavigation> query;
        static InitQuery()
        {
            query = ClassWithDeepNavigation.CreateQuery();

            ODataConventionModelBuilder builder = new ODataConventionModelBuilder();
            builder.AddEntityType(typeof(ClassWithDeepNavigation));
            builder.AddEntitySet(typeof(ClassWithDeepNavigation).Name, new EntityTypeConfiguration(new ODataModelBuilder(), typeof(ClassWithDeepNavigation)));
            defaultEdmModel = builder.GetEdmModel();
        }

        [Benchmark]
        public Tuple<IQueryable, ServiceContainer> CreateLegacy()
        {
            ODataConventionModelBuilder builder = new ODataConventionModelBuilder();
            builder.AddEntityType(typeof(ClassWithDeepNavigation));
            builder.AddEntitySet(typeof(ClassWithDeepNavigation).Name, new EntityTypeConfiguration(new ODataModelBuilder(), typeof(ClassWithDeepNavigation)));
            var edmModel = builder.GetEdmModel();


            ODataSettings settings = new ODataSettings();

            ServiceContainer container = new ServiceContainer();
            container.AddService(typeof(IEdmModel), edmModel);
            container.AddService(typeof(ODataQuerySettings), settings.QuerySettings);
            container.AddService(typeof(ODataUriParserSettings), settings.ParserSettings);
            container.AddService(typeof(FilterBinder), new FilterBinder(container));
            container.AddService(typeof(ODataUriResolver), settings.Resolver ?? DefaultResolver);
            container.AddService(typeof(ODataSimplifiedOptions), SimplifiedOptions);
            container.AddService(typeof(ODataSettings), settings);
            container.AddService(typeof(DefaultQuerySettings), settings.DefaultQuerySettings);
            container.AddService(typeof(SelectExpandQueryValidator), new SelectExpandQueryValidator(settings.DefaultQuerySettings));

            return new Tuple<IQueryable, ServiceContainer>(query, container);
        }

        [Benchmark]
        public Tuple<IQueryable, ServiceContainer> CreateNoContainer()
        {
            var edmModel = defaultEdmModel;

            if (edmModel.SchemaElements.Count(e => e.SchemaElementKind == EdmSchemaElementKind.EntityContainer) == 0)
            {
                throw new ArgumentException("Provided Entity Model have no IEdmEntityContainer", nameof(edmModel));
            }

            ODataSettings settings = new ODataSettings();
            ServiceContainer container = new ServiceContainer();
            
            container.AddService(typeof(IEdmModel), edmModel);
            container.AddService(typeof(ODataQuerySettings), settings.QuerySettings);
            container.AddService(typeof(ODataUriParserSettings), settings.ParserSettings);
            container.AddService(typeof(FilterBinder), new FilterBinder(container));
            container.AddService(typeof(ODataUriResolver), settings.Resolver ?? DefaultResolver);
            container.AddService(typeof(ODataSimplifiedOptions), SimplifiedOptions);
            container.AddService(typeof(ODataSettings), settings);
            container.AddService(typeof(DefaultQuerySettings), settings.DefaultQuerySettings);
            container.AddService(typeof(SelectExpandQueryValidator), new SelectExpandQueryValidator(settings.DefaultQuerySettings));            

            return new Tuple<IQueryable, ServiceContainer>(query, container);
        }

        [Benchmark]
        public Tuple<IQueryable, ServiceContainer> CreateExtension()
        {
            return new Tuple<IQueryable, ServiceContainer>(query.OData(), new ServiceContainer());
        }
    }
}
