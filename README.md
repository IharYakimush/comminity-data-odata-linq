# Community.OData.Linq
Use OData filter text query in linq expresson for any IQuerable without ASP.NET dependency

# Sample
Please check simple code below to get started:
### .NET Fiddle
https://dotnetfiddle.net/7Ndwot
### Code
```
    using System;
    using System.Linq;

    using Community.OData.Linq;

    public class Entity
    {
        public int Id { get; set; }

        public string Name { get; set; }
    }

    public static class GetStartedDemo
    {
        public static void Demo()
        {
            Entity[] items =
                {
                    new Entity { Id = 1, Name = "n1" },
                    new Entity { Id = 2, Name = "n2" },
                    new Entity { Id = 3, Name = "n3" }
                };
            IQueryable<Entity> query = items.AsQueryable();

            var result = query.OData().Filter("Id eq 1 or Name eq 'n3'").OrderBy("Name desc").TopSkip("10", "0").ToArray();

            // Id: 3 Name: n3
            // Id: 1 Name: n1
            foreach (Entity entity in result)
            {
                Console.WriteLine("Id: {0} Name: {1}", entity.Id, entity.Name);
            }
        }
    }
```
# Advanced code samples at wiki
https://github.com/IharYakimush/comminity-data-odata-linq/wiki

# Supported OData parameters
- $filter
- $orderby
- $select
- $expand
- $top
- $skip

# Nuget
- https://www.nuget.org/packages/Community.OData.Linq
- https://www.nuget.org/packages/Community.OData.Linq.Json

# Contribution
Please feel free to create issues and pool requests to develop branch

# Build Status
[![Build status](https://ci.appveyor.com/api/projects/status/yrmp3074ryce61gb/branch/develop?svg=true)](https://ci.appveyor.com/project/IharYakimush/comminity-data-odata-linq/branch/develop)

# References
Majority of the code was taken from https://github.com/OData/WebApi
