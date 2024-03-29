﻿

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

            Student[] array = c.Students.OData().Filter("LastName eq 'Alexander' or FirstMidName eq 'Laura'").OrderBy("EnrollmentDate desc").TopSkip("1","1").ToOriginalQuery().ToArrayAsync().Result;

            Console.WriteLine(array.Length);

            ISelectExpandWrapper[] select1 = c.Students.OData().Filter("LastName eq 'Alexander' or FirstMidName eq 'Laura'").OrderBy("EnrollmentDate desc").SelectExpand("LastName").ToArray();

            Console.WriteLine(select1.ToJson());

            ISelectExpandWrapper[] select2 = c.Students.OData().Filter("LastName eq 'Alexander' or FirstMidName eq 'Laura'").OrderBy("EnrollmentDate desc").SelectExpandAsQueryable("LastName", "Enrollments($select=CourseId)").ToArrayAsync().Result;

            Console.WriteLine(select2.ToJson());
        }
    }
}
