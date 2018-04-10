namespace Demo
{
    using System;

    using Community.OData.Linq;

    class Program
    {
        static void Main(string[] args)
        {
            ODataSettings.SetInitializer(s => s.ValidationSettings.MaxTop = 1000);

            GetStartedDemo.Demo();
            Console.WriteLine();

            FilterDemo.BySimpleProperties();                        
            FilterDemo.ByRelatedEntity();
            OrderByDemo.BySimpleProperties();
            SelectDemo.OnlyNameField();

            ExpandDemo.SelectExpand1();
            ExpandDemo.SelectExpand2();

            SelectExpandJsonDemo.SelectExpandToJson();
        }
    }
}
