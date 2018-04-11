namespace Community.OData.Linq.AspNetCore.IntegrationTests.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.AspNetCore.Mvc;
    using Community.OData.Linq.Json;

    using Microsoft.OData;

    using Newtonsoft.Json.Linq;

    public class ValuesController : Controller
    {
        [Route("/v1")]
        [HttpGet]
        public IActionResult Get1(ODataQueryOptions queryOptions)
        {
            if (queryOptions == null && !this.ModelState.IsValid )
            {
                return this.BadRequest(this.ModelState);
            }

            IQueryable<SampleData> data = Enumerable.Range(1, 10)
                .Select(i => new SampleData { Id = i, Name = $"n{i}" })
                .ToArray()
                .AsQueryable();

            try
            {
                SampleData[] result = data.OData().ApplyRawQueryOptionsWithoutSelectExpand(queryOptions).ToArray();
                return this.Ok(result);
            }
            catch (ODataException e)
            {
                this.ModelState.TryAddModelError("odata", e.Message);
                return this.BadRequest(this.ModelState);
            }
        }

        [Route("/v2")]
        [HttpGet]
        public IActionResult Get2(ODataQueryOptions queryOptions)
        {
            if (queryOptions == null && !this.ModelState.IsValid)
            {
                return this.BadRequest(this.ModelState);
            }

            IQueryable<SampleData> data = Enumerable.Range(1, 10)
                .Select(i => new SampleData { Id = i, Name = $"n{i}" })
                .ToArray()
                .AsQueryable();
            try
            {
                JToken result = data.OData().ApplyRawQueryOptionsWithSelectExpandJsonToken(queryOptions);
                return this.Ok(result);
            }
            catch (ODataException e)
            {
                return this.BadRequest(e.Message);
            }
        }        
    }
}