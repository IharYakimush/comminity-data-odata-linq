# Community.OData.Linq
Use OData filter text query in linq expresson for any IQuerable. Support web and desktop applications.

# Sample
Please check samples below to get started:
### .NET Fiddle
https://dotnetfiddle.net/7Ndwot
## Console app
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
## Support ToArrayAsync(), ToListAsync(), and all other provider specific methods.
Use `.ToOriginalQuery()` after finishing working with OData to be able to support provider specific methods of original query.

### Entity Framework async data fetch.
```
Student[] array = await dbContext.Students.OData()
                .Filter("LastName eq 'Alexander' or FirstMidName eq 'Laura'")
                .OrderBy("EnrollmentDate desc")
                .TopSkip("1","1")
                .ToOriginalQuery() // required to be able to use .ToArrayAsync() next.
                .ToArrayAsync();

ISelectExpandWrapper[] select2 = await dbContext.Students.OData()
                .Filter("LastName eq 'Alexander' or FirstMidName eq 'Laura'")
                .OrderBy("EnrollmentDate desc")
                .SelectExpandAsQueryable("LastName", "Enrollments($select=CourseId)") //.SelectExpandAsQueryable() use .ToOriginalQuery() implicitly, so not need to call it.
                .ToArrayAsync()
```
### CosmosDb SQL API async data fetch.
```
var item = await Container.GetItemLinqQueryable<TestEntity>().OData()
                .Filter($"Id eq '{id1}'")
                .TopSkip("1")
                .ToOriginalQuery() // required to be able to use .ToFeedIterator() next.
                .ToFeedIterator()
                .ReadNextAsync()
```
# Advanced code samples at wiki
https://github.com/IharYakimush/comminity-data-odata-linq/wiki

# Supported OData parameters
| Params        | In Memory Collections | Entity Framework | CosmosDB SQL API |
| ------------- |:---------------------:|:----------------:| :---------------:|
| $filter       |+                      | +                | +                |
| $orderby      |+                      | +                | +                |
| $select       |+                      | +                | -                |
| $expand       |+                      | +                | -                |
| $top          |+                      | +                | +                |
| $skip         |+                      | +                | +                |

# Nuget
- https://www.nuget.org/packages/Community.OData.Linq
- https://www.nuget.org/packages/Community.OData.Linq.Json
- https://www.nuget.org/packages/Community.OData.Linq.AspNetCore

# Contribution
Please feel free to create issues and pool requests to develop branch

# Build Status
[![Build status](https://ci.appveyor.com/api/projects/status/yrmp3074ryce61gb/branch/develop?svg=true)](https://ci.appveyor.com/project/IharYakimush/comminity-data-odata-linq/branch/develop)

# References
Majority of the code was taken from https://github.com/OData/WebApi
