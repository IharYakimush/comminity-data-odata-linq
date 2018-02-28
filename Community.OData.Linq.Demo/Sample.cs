namespace Community.OData.Linq.Demo
{
    using System.Collections.Generic;
    using System.Linq;

    public class Sample
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public Sample RelatedEntity { get; set; }

        public ICollection<Sample> RelatedEntitiesCollection { get; set; }

        public static IQueryable<Sample> CreateQuerable()
        {
            var items = new[]
              {
                  new Sample
                      {
                          Id = 1, Name = "name1",
                          RelatedEntity = new Sample { Id = 10, Name = "name10" },
                          RelatedEntitiesCollection =
                              new List<Sample>
                                  {
                                      new Sample { Id = 100, Name = "name100" },
                                      new Sample { Id = 101, Name = "name101" }
                                  }
                      },
                  new Sample
                      {
                          Id = 2, Name = "name2",
                          RelatedEntity = new Sample { Id = 20, Name = "name20" },
                          RelatedEntitiesCollection =
                              new List<Sample>
                                  {
                                      new Sample { Id = 200, Name = "name200" },
                                      new Sample { Id = 201, Name = "name201" }
                                  }
                      },
                  new Sample
                      {
                          Id = 3, Name = "name3",
                          RelatedEntity = new Sample { Id = 30, Name = "name30" },
                          RelatedEntitiesCollection =
                              new List<Sample>
                                  {
                                      new Sample { Id = 300, Name = "name300" },
                                      new Sample { Id = 301, Name = "name301" }
                                  }
                      }
              };

            return items.AsQueryable();
        }
    }
}