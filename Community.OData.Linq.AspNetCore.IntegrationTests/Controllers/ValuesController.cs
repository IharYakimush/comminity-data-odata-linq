namespace Community.OData.Linq.AspNetCore.IntegrationTests
{
    using System.Linq;

    using Community.OData.Linq;
    using Community.OData.Linq.AspNetCore;

    using Community.OData.Linq.Json;

    using Microsoft.AspNetCore.Mvc;
    using Microsoft.OData;

    using Newtonsoft.Json.Linq;

    public class ValuesController : Controller
    {
        [Route("/v1")]
        [HttpGet]
        public IActionResult Get(ODataQueryOptions queryOptions)
        {
            if (queryOptions == null && !this.ModelState.IsValid)
            {
                return this.BadRequest(this.ModelState);
            }

            IQueryable<Entity> data = Enumerable.Range(1, 10)
                .Select(i => new Entity { Id = i, Name = $"n{i}" })
                .ToArray()
                .AsQueryable();
            try
            {
                JToken result = data.OData().ApplyQueryOptions(queryOptions).ToJson();
                return this.Ok(result);
            }
            catch (ODataException e)
            {
                return this.BadRequest(e.Message);
            }
        }

        [Route("/v2")]
        [HttpGet]
        public IActionResult GetWithoutSelectExpand(ODataQueryOptions queryOptions)
        {
            if (queryOptions == null && !this.ModelState.IsValid)
            {
                return this.BadRequest(this.ModelState);
            }

            IQueryable<Entity> data = Enumerable.Range(1, 10)
                .Select(i => new Entity { Id = i, Name = $"n{i}" })
                .ToArray()
                .AsQueryable();

            try
            {
                Entity[] result = data.OData().ApplyQueryOptionsWithoutSelectExpand(queryOptions).ToArray();
                return this.Ok(result);
            }
            catch (ODataException e)
            {
                return this.BadRequest(e.Message);
            }
        }
    }
}