# Community.OData.Linq
Use text (OData format) expressons in LINQ methods for any IQuerable

# Sample
Please check simple code below to get started
```
    using System;
    using System.Linq;
    using Community.OData.Linq;

    public class Sample
    {        
        public int Id { get; set; }

        public string Name { get; set; }        
    }

    public class Demo
    {                
        public void SimpleFilter()
        {
            IQueryable<Sample> dataSet = this.CreateQuerable();
            Sample[] filterResult = dataSet.OData().Filter("Id eq 2 or Name eq 'name3'").ToArray();

            // Id:2 Name:name2
            // Id:3 Name:name3
            foreach (Sample sample in filterResult)
            {
                Console.WriteLine(string.Format("Id:{0} Name:{1}", sample.Id, sample.Name));
            }
        }

        public void SimpleOrderBy()
        {
            IQueryable<Sample> dataSet = this.CreateQuerable();
            Sample[] filterResult = dataSet.OData().OrderBy("Id desc").ToArray();

            // Id:3 Name:name3
            // Id:2 Name:name2
            // Id:1 Name:name1
            foreach (Sample sample in filterResult)
            {
                Console.WriteLine(string.Format("Id:{0} Name:{1}", sample.Id, sample.Name));
            }
        }

        private IQueryable<Sample> CreateQuerable()
        {
            return new[]
                       {
                           new Sample { Id = 1, Name = "name1" },
                           new Sample { Id = 2, Name = "name2" },
                           new Sample { Id = 3, Name = "name3" },
                       }.AsQueryable();
        }
    }
```
# Nuget
https://www.nuget.org/packages/Community.OData.Linq

# Build Status
[![Build status](https://ci.appveyor.com/api/projects/status/yrmp3074ryce61gb/branch/develop?svg=true)](https://ci.appveyor.com/project/IharYakimush/comminity-data-odata-linq/branch/develop)
