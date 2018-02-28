namespace Community.OData.Linq.Demo
{
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
}