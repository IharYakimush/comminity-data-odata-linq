namespace Demo
{
    using System;
    using System.Linq;

    using Community.OData.Linq;
    using Community.OData.Linq.Json;

    using Newtonsoft.Json;

    public static class SelectExpandJsonDemo
    {
        public static void SelectExpandToJson()
        {
            Console.WriteLine(nameof(SelectExpandToJson));
            Console.WriteLine("/*");

            IQueryable<Sample> dataSet = Sample.CreateQuerable();
            var result = dataSet.OData().SelectExpand(
                "Name",
                "RelatedEntity($select=Id),RelatedEntitiesCollection($filter=Id ge 200;$top=1)").ToJson();

            /*
            [
              {
                "Name": "name1",
                "RelatedEntitiesCollection": [],
                "RelatedEntity": {
                  "Id": 10
                }
              },
              {
                "Name": "name2",
                "RelatedEntitiesCollection": [
                  {
                    "Id": 200,
                    "Name": "name200"
                  }
                ],
                "RelatedEntity": {
                  "Id": 20
                }
              },
              {
                "Name": "name3",
                "RelatedEntitiesCollection": [
                  {
                    "Id": 300,
                    "Name": "name300"
                  }
                ],
                "RelatedEntity": {
                  "Id": 30
                }
              }
            ]
            */
            Console.WriteLine(result.ToString(Formatting.Indented));

            Console.WriteLine("*/");
            Console.WriteLine(Environment.NewLine);
        }
    }
}