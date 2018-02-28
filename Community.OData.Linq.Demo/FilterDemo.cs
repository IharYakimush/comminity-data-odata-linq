namespace Demo
{
    using System;
    using System.Linq;

    using Community.OData.Linq;

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
}