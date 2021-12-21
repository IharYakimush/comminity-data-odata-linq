

namespace Community.OData.Linq.EF6Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using ContosoUniversity.DAL;
    using ContosoUniversity.Migrations;
    using Community.OData.Linq.Json;
    using System.Data.Entity;
    using ContosoUniversity.Models;

    public class Program
    {
        public static void Main()
        {
            SchoolContext c = new SchoolContext();
            
            c.Database.Initialize(true);
            Configuration configuration = new Configuration();
            configuration.Seed2(c);

            Console.WriteLine(c.Students.Count());
            var result = c.Students.OData().Filter("LastName eq 'Alexander' or FirstMidName eq 'Laura'").OrderBy("EnrollmentDate desc").SelectExpand().ToJson();

            Console.WriteLine(result.ToString(formatting: Newtonsoft.Json.Formatting.Indented));

            Student[] array = c.Students.OData().Filter("LastName eq 'Alexander' or FirstMidName eq 'Laura'").OrderBy("EnrollmentDate desc").ToOriginalQuery().ToArrayAsync().Result;

            Console.WriteLine(array.Length);
        }
    }
}
