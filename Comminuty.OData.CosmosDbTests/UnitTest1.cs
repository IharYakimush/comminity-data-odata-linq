using Comminuty.OData.CosmosDbTests.Models;

using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.Extensions.Configuration;

using System.Linq;
using System.Threading.Tasks;

using Community.OData.Linq;

using Xunit;
using Microsoft.Azure.Cosmos.Linq;

namespace Comminuty.OData.CosmosDbTests
{
    public class UnitTest1
    {
        static IConfiguration configuration;
        static UnitTest1()
        {
            IConfigurationBuilder builder = new ConfigurationBuilder().AddUserSecrets(typeof(UnitTest1).Assembly);
            configuration = builder.Build();
        }

        [Fact]
        public async Task LinqSync()
        {
            using (CosmosClient client = new CosmosClientBuilder(configuration["CosmosDb"])
                .WithSerializerOptions(
                new CosmosSerializationOptions()
                {
                    PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                }).Build())
            {
                var db = await client.CreateDatabaseIfNotExistsAsync("dam");
                var container = await db.Database.CreateContainerIfNotExistsAsync("odata", "/pk");

                TestEntity entity = TestEntity.Create();
                await container.Container.CreateItemAsync(entity);
                string id1 = entity.Id;

                entity = TestEntity.Create();
                await container.Container.CreateItemAsync(entity);
                string id2 = entity.Item.Id;

                entity = TestEntity.Create();
                await container.Container.CreateItemAsync(entity);
                string id3 = entity.Childs.First().Id;


                var query = container.Container.GetItemLinqQueryable<TestEntity>(true);

                var single1 = query.Where(e => e.Id == id1).AsEnumerable().SingleOrDefault();
                var single2 = query.Where(e => e.Item.Id == id2).AsEnumerable().SingleOrDefault();
                var single3 = query.Where(e => e.Childs.Any(c => c.Id == id3)).AsEnumerable().SingleOrDefault();

                Assert.NotNull(single1);
                Assert.Equal(id1, single1.Id);

                Assert.NotNull(single2);
                Assert.Equal(id2, single2.Item.Id);

                Assert.NotNull(single3);
                Assert.Equal(entity.Id, single3.Id);
                Assert.Contains(single3.Childs, c => c.Id == id3);
            }
        }

        [Fact]
        public async Task ODataSync()
        {
            using (CosmosClient client = new CosmosClientBuilder(configuration["CosmosDb"])
                .WithSerializerOptions(
                new CosmosSerializationOptions()
                {
                    PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                }).Build())
            {
                var db = await client.CreateDatabaseIfNotExistsAsync("dam");
                var container = await db.Database.CreateContainerIfNotExistsAsync("odata", "/pk");

                TestEntity entity = TestEntity.Create();
                await container.Container.CreateItemAsync(entity);
                string id1 = entity.Id;

                entity = TestEntity.Create();
                await container.Container.CreateItemAsync(entity);
                string id2 = entity.Item.Id;

                entity = TestEntity.Create();
                await container.Container.CreateItemAsync(entity);
                string id3 = entity.Childs.First().Id;
                
                ODataQuery<TestEntity> odataQuery = container.Container.GetItemLinqQueryable<TestEntity>(true).OData();

                var single1 = odataQuery.Filter($"Id eq '{id1}'").AsEnumerable().SingleOrDefault();
                Assert.NotNull(single1);
                Assert.Equal(id1, single1.Id);

                var single2 = odataQuery.Filter($"Item/Id eq '{id2}'").AsEnumerable().SingleOrDefault();
                Assert.NotNull(single2);
                Assert.Equal(id2, single2.Item.Id);

                var single3 = odataQuery.Filter($"Childs/Any(c: c/Id eq '{id3}')").AsEnumerable().SingleOrDefault();
                Assert.NotNull(single3);
                Assert.Contains(single3.Childs, c => c.Id == id3);
            }
        }

        [Fact]
        public async Task ODataAsync()
        {
            using (CosmosClient client = new CosmosClientBuilder(configuration["CosmosDb"])
                .WithSerializerOptions(
                new CosmosSerializationOptions()
                {
                    PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                }).Build())
            {
                var db = await client.CreateDatabaseIfNotExistsAsync("dam");
                var container = await db.Database.CreateContainerIfNotExistsAsync("odata", "/pk");

                TestEntity entity = TestEntity.Create();
                await container.Container.CreateItemAsync(entity);
                string id1 = entity.Id;

                entity = TestEntity.Create();
                await container.Container.CreateItemAsync(entity);
                string id2 = entity.Item.Id;

                entity = TestEntity.Create();
                await container.Container.CreateItemAsync(entity);
                string id3 = entity.Childs.First().Id;

                ODataQuery<TestEntity> odataQuery = container.Container.GetItemLinqQueryable<TestEntity>(true).OData();

                var single1 = (await odataQuery.Filter($"Id eq '{id1}'").ToOriginalQuery().ToFeedIterator().ReadNextAsync()).SingleOrDefault();
                Assert.NotNull(single1);
                Assert.Equal(id1, single1.Id);

                var single2 = (await odataQuery.Filter($"Item/Id eq '{id2}'").ToOriginalQuery().ToFeedIterator().ReadNextAsync()).SingleOrDefault();
                Assert.NotNull(single2);
                Assert.Equal(id2, single2.Item.Id);

                var single3 = (await odataQuery.Filter($"Childs/Any(c: c/Id eq '{id3}')").ToOriginalQuery().ToFeedIterator().ReadNextAsync()).SingleOrDefault();
                Assert.NotNull(single3);
                Assert.Contains(single3.Childs, c => c.Id == id3);
            }
        }

        [Fact]
        public async Task LinqAsync()
        {
            using (CosmosClient client = new CosmosClientBuilder(configuration["CosmosDb"])
                .WithSerializerOptions(
                new CosmosSerializationOptions()
                {
                    PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                }).Build())
            {
                var db = await client.CreateDatabaseIfNotExistsAsync("dam");
                var container = await db.Database.CreateContainerIfNotExistsAsync("odata", "/pk");

                TestEntity entity = TestEntity.Create();
                await container.Container.CreateItemAsync(entity);
                string id1 = entity.Id;

                entity = TestEntity.Create();
                await container.Container.CreateItemAsync(entity);
                string id2 = entity.Item.Id;

                entity = TestEntity.Create();
                await container.Container.CreateItemAsync(entity);
                string id3 = entity.Childs.First().Id;


                var query = container.Container.GetItemLinqQueryable<TestEntity>();

                var single1 = (await query.Where(e => e.Id == id1).ToFeedIterator().ReadNextAsync()).SingleOrDefault();
                var single2 = (await query.Where(e => e.Item.Id == id2).ToFeedIterator().ReadNextAsync()).SingleOrDefault();
                var single3 = (await query.Where(e => e.Childs.Any(c => c.Id == id3)).ToFeedIterator().ReadNextAsync()).SingleOrDefault();

                Assert.NotNull(single1);
                Assert.Equal(id1, single1.Id);

                Assert.NotNull(single2);
                Assert.Equal(id2, single2.Item.Id);

                Assert.NotNull(single3);
                Assert.Equal(entity.Id, single3.Id);
                Assert.Contains(single3.Childs, c => c.Id == id3);
            }
        }
    }
}