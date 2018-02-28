# Community.OData.Linq
Use OData filter text query in linq expresson for any IQuerable without ASP.NET dependency

# Sample
Please check simple code below to get started:

Class with some properties and navigation links:
```
    using System.Collections.Generic;
    using System.Linq;

    public class Sample
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public Sample LinkedSample { get; set; }

        public ICollection<Sample> LinkedCollection { get; set; }

        public static IQueryable<Sample> CreateQuerable()
        {
            var items = new[]
              {
                  new Sample
                      {
                          Id = 1, Name = "name1",
                          LinkedSample = new Sample { Id = 10, Name = "name10" },
                          LinkedCollection =
                              new List<Sample>
                                  {
                                      new Sample { Id = 100, Name = "name100" },
                                      new Sample { Id = 101, Name = "name101" }
                                  }
                      },
                  new Sample
                      {
                          Id = 2, Name = "name2",
                          LinkedSample = new Sample { Id = 20, Name = "name20" },
                          LinkedCollection =
                              new List<Sample>
                                  {
                                      new Sample { Id = 200, Name = "name200" },
                                      new Sample { Id = 201, Name = "name201" }
                                  }
                      },
                  new Sample
                      {
                          Id = 3, Name = "name3",
                          LinkedSample = new Sample { Id = 30, Name = "name30" },
                          LinkedCollection =
                              new List<Sample>
                                  {
                                      new Sample { Id = 300, Name = "name300" },
                                      new Sample { Id = 301, Name = "name301" }
                                  }
                      }
              };

            return items.AsQueryable();
        }
    }
```
Filter
```
    using System;
    using System.Linq;

    public static class FilterDemo
    {
        public static void BySimpleProperties()
        {
            Console.WriteLine(nameof(BySimpleProperties));

            IQueryable<Sample> dataSet = Sample.CreateQuerable();
            Sample[] filterResult = dataSet.OData().Filter("Id eq 2 or Name eq 'name3'").ToArray();

            // Id:2 Name:name2
            // Id:3 Name:name3
            foreach (Sample sample in filterResult)
            {
                Console.WriteLine(string.Format("Id:{0} Name:{1}", sample.Id, sample.Name));
            }

            Console.WriteLine(Environment.NewLine);
        }

        public static void ByRelatedEntity()
        {
            Console.WriteLine(nameof(ByRelatedEntity));

            IQueryable<Sample> dataSet = Sample.CreateQuerable();
            Sample[] filterResult = dataSet.OData().Filter("RelatedEntity/Id eq 10").ToArray();

            // Id: 1 Name: name1
            foreach (Sample sample in filterResult)
            {
                Console.WriteLine(string.Format("Id:{0} Name:{1}", sample.Id, sample.Name));
            }

            Console.WriteLine(Environment.NewLine);
        }
    }
```
OrderBy
```
	using System;
    using System.Linq;

    public static class OrderByDemo
    {
        public static void BySimpleProperties()
        {
            Console.WriteLine(nameof(BySimpleProperties));

            IQueryable<Sample> dataSet = Sample.CreateQuerable();
            Sample[] filterResult = dataSet.OData().OrderBy("Id desc").ToArray();

            // Id:3 Name:name3
            // Id:2 Name:name2
            // Id:1 Name:name1
            foreach (Sample sample in filterResult)
            {
                Console.WriteLine("Id:{0} Name:{1}", sample.Id, sample.Name);
            }

            Console.WriteLine(Environment.NewLine);
        }
    }
```
Select
```
	using System;
    using System.Collections.Generic;
    using System.Linq;

    public static class SelectDemo
    {
        public static void OnlyNameField()
        {
            Console.WriteLine(nameof(OnlyNameField));

            IQueryable<Sample> dataSet = Sample.CreateQuerable();
            ISelectExpandWrapper[] filterResult = dataSet.OData().SelectExpand("Name").ToArray();

            /*
            Name: name1
            ---
            Name: name2
            ---
            Name: name3
            ---
            */
            foreach (var sample in filterResult)
            {
                var metadata = sample.ToDictionary();
                foreach (KeyValuePair<string, object> pair in metadata)
                {
                    Console.WriteLine("{0}: {1}", pair.Key, pair.Value);
                }

                Console.WriteLine("---");
            }

            Console.WriteLine(Environment.NewLine);
        }
    }
```
Expand
```
	using System;
    using System.Collections.Generic;
    using System.Linq;

    public static class ExpandDemo
    {                
        public static void SelectExpand1()
        {
            Console.WriteLine(nameof(SelectExpand1));
            Console.WriteLine("/*");

            IQueryable<Sample> dataSet = Sample.CreateQuerable();
            ISelectExpandWrapper[] filterResult = dataSet.OData().SelectExpand(
                "Name",
                "RelatedEntity($select=Id),RelatedEntitiesCollection($filter=Id ge 200;$top=1)").ToArray();

            /*
            |-Name: name1
            |-RelatedEntitiesCollection:
            |-RelatedEntity:
            |  |-Id: 10
            |  |---

            |-Name: name2
            |-RelatedEntitiesCollection:
            |  |-Id: 200
            |  |-Name: name200
            |  |---
            |-RelatedEntity:
            |  |-Id: 20
            |  |---

            |-Name: name3
            |-RelatedEntitiesCollection:
            |  |-Id: 300
            |  |-Name: name300
            |  |---
            |-RelatedEntity:
            |  |-Id: 30
            |  |---
            */
            foreach (var sample in filterResult)
            {
                PrintRecursive(sample);
            }

            Console.WriteLine("*/");
            Console.WriteLine(Environment.NewLine);
        }

        public static void SelectExpand2()
        {
            Console.WriteLine(nameof(SelectExpand2));
            Console.WriteLine("/*");

            IQueryable<Sample> dataSet = Sample.CreateQuerable();
            ISelectExpandWrapper[] filterResult = dataSet.OData().SelectExpand(
                "Id",
                "RelatedEntity,RelatedEntitiesCollection($top=2)").ToArray();

            /*
            |-Id: 1
            |-RelatedEntitiesCollection:
            |  |-Id: 100
            |  |-Name: name100
            |  |---
            |  |-Id: 101
            |  |-Name: name101
            |  |---
            |-RelatedEntity:
            |  |-Id: 10
            |  |-Name: name10
            |  |---

            |-Id: 2
            |-RelatedEntitiesCollection:
            |  |-Id: 200
            |  |-Name: name200
            |  |---
            |  |-Id: 201
            |  |-Name: name201
            |  |---
            |-RelatedEntity:
            |  |-Id: 20
            |  |-Name: name20
            |  |---

            |-Id: 3
            |-RelatedEntitiesCollection:
            |  |-Id: 300
            |  |-Name: name300
            |  |---
            |  |-Id: 301
            |  |-Name: name301
            |  |---
            |-RelatedEntity:
            |  |-Id: 30
            |  |-Name: name30
            |  |---
            */
            foreach (var sample in filterResult)
            {
                PrintRecursive(sample);
            }

            Console.WriteLine("*/");
            Console.WriteLine(Environment.NewLine);
        }

        private static void PrintRecursive(ISelectExpandWrapper wrapper, string indent = "|")
        {
            var metadata = wrapper.ToDictionary();
            foreach (KeyValuePair<string, object> pair in metadata)
            {
                if (pair.Value is ISelectExpandWrapper property)
                {
                    Console.WriteLine($"{indent}-{pair.Key}:");
                    PrintRecursive(property, indent + "  |");
                    continue;
                }

                if (pair.Value is IEnumerable<ISelectExpandWrapper> collection)
                {
                    Console.WriteLine($"{indent}-{pair.Key}:");
                    foreach (ISelectExpandWrapper item in collection)
                    {
                        PrintRecursive(item, indent + "  |");
                    }

                    continue;
                }

                Console.WriteLine($"{indent}-{pair.Key}: {pair.Value}");
            }

            if (indent == "|")
            {
                Console.WriteLine();
            }
            else
            {
                Console.WriteLine(indent + "---");
            }
        }
    }
```
# Nuget
https://www.nuget.org/packages/Community.OData.Linq

# Contribution
Please feel free to create issues and pool requests to develop branch

# Build Status
[![Build status](https://ci.appveyor.com/api/projects/status/yrmp3074ryce61gb/branch/develop?svg=true)](https://ci.appveyor.com/project/IharYakimush/comminity-data-odata-linq/branch/develop)

# References
Majority of the code was taken from https://github.com/OData/WebApi
