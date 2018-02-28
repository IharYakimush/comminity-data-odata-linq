namespace Community.OData.Linq.Demo
{
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
}