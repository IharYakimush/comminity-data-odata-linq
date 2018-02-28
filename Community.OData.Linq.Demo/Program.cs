namespace Community.OData.Linq.Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            FilterDemo.BySimpleProperties();                        
            FilterDemo.ByRelatedEntity();
            OrderByDemo.BySimpleProperties();
            SelectDemo.OnlyNameField();

            ExpandDemo.SelectExpand1();
            ExpandDemo.SelectExpand2();
        }
    }
}
