namespace Demo
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Community.OData.Linq;

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
}
 