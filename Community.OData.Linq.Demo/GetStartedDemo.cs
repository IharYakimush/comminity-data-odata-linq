namespace Demo
{
    using System;
    using System.Linq;

    using Community.OData.Linq;

    public class Entity
    {
        public int Id { get; set; }

        public string Name { get; set; }
    }

    public static class GetStartedDemo
    {
        public static void Demo()
        {
            Entity[] items =
                {
                    new Entity { Id = 1, Name = "n1" },
                    new Entity { Id = 2, Name = "n2" },
                    new Entity { Id = 3, Name = "n3" }
                };
            IQueryable<Entity> query = items.AsQueryable();

            var result = query.OData().Filter("Id eq 1 or Name eq 'n3'").OrderBy("Name desc").ToArray();

            // Id: 3 Name: n3
            // Id: 1 Name: n1
            foreach (Entity entity in result)
            {
                Console.WriteLine("Id: {0} Name: {1}", entity.Id, entity.Name);
            }
        }
    }
}