namespace Demo
{
    using System;

    class Program
    {
        static void Main(string[] args)
        {
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
