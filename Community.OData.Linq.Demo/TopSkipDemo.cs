namespace Demo
{
    using System;
    using System.Linq;

    using Community.OData.Linq;

    public static class TopSkipDemo
    {
        public static void DefaultPageSize()
        {
            Console.WriteLine(nameof(DefaultPageSize));

            IQueryable<Sample> dataSet = Enumerable.Range(1, 50).Select(i => new Sample { Id = i }).AsQueryable();
            Sample[] filterResult = dataSet.OData(s => s.QuerySettings.PageSize = 10).TopSkip().ToArray();

            // 1-10
            foreach (Sample sample in filterResult)
            {
                Console.WriteLine(string.Format("Id:{0}", sample.Id));
            }

            Console.WriteLine(Environment.NewLine);
        }

        public static void Top5()
        {
            Console.WriteLine(nameof(Top5));

            IQueryable<Sample> dataSet = Enumerable.Range(1, 50).Select(i => new Sample { Id = i }).AsQueryable();
            Sample[] filterResult = dataSet.OData().TopSkip("5").ToArray();

            // 1-5
            foreach (Sample sample in filterResult)
            {
                Console.WriteLine(string.Format("Id:{0}", sample.Id));
            }

            Console.WriteLine(Environment.NewLine);
        }

        public static void Top5Skip5()
        {
            Console.WriteLine(nameof(Top5Skip5));

            IQueryable<Sample> dataSet = Enumerable.Range(1, 50).Select(i => new Sample { Id = i }).AsQueryable();
            Sample[] filterResult = dataSet.OData().TopSkip("5", "5").ToArray();

            // 6-10
            foreach (Sample sample in filterResult)
            {
                Console.WriteLine(string.Format("Id:{0}", sample.Id));
            }

            Console.WriteLine(Environment.NewLine);
        }
    }
}