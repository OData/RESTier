using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.OData.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;

namespace Microsoft.OData.Service.Sample.TrippinInMemory.Models
{
    public class TrippinApi : ApiBase
    {
        private static readonly List<Person> people = new List<Person>
        {
            new Person
            {
                PersonId = 1,
                FirstName = "u1",
                FavoriteFeature = Feature.Feature1,
                Emails = new Collection<string>
                {
                    "u1@trippin.com",
                    "u1@odata.org"
                },
                HomeAddress = new Location { Address = "ccc1" },
                Locations = new Collection<Location>
                {
                    new Location { Address = "a1" },
                    new Location { Address = "b1" }
                },
                Features = new Collection<Feature>
                {
                    Feature.Feature1,
                    Feature.Feature3
                }
            },
            new Person
            {
                PersonId = 2,
                FirstName = "u2",
                FavoriteFeature = Feature.Feature3,
                Emails = new Collection<string>
                {
                    "u2@trippin.com",
                    "u2@odata.org"
                },
                Locations = new Collection<Location>
                {
                    new Location { Address = "a2" },
                    new Location { Address = "b2" }
                },
                Features = new Collection<Feature>
                {
                    Feature.Feature3,
                    Feature.Feature2
                }
            },
            new Person
            {
                PersonId = 3,
                FirstName = "u3",
                FavoriteFeature = Feature.Feature2,
                Emails = new Collection<string>
                {
                    "u3@trippin.com",
                    "u3@odata.org"
                },
                Locations = new Collection<Location>
                {
                    new Location { Address = "a3" },
                    new Location { Address = "b3" }
                },
                Features = new Collection<Feature>
                {
                    Feature.Feature2,
                    Feature.Feature4
                }
            },
            new Person
            {
                PersonId = 4,
                FirstName = "u4",
                FavoriteFeature = Feature.Feature4,
                Emails = new Collection<string>
                {
                    "u4@trippin.com",
                    "u4@odata.org"
                },
                Locations = new Collection<Location>
                {
                    new Location { Address = "a4" },
                    new Location { Address = "b4" }
                },
                Features = new Collection<Feature>
                {
                    Feature.Feature4,
                    Feature.Feature1
                }
            },
            new Person
            {
                PersonId = 5,
                FirstName = "u4",
                FavoriteFeature = Feature.Feature4,
                Emails = new Collection<string>(),
                Features = new Collection<Feature>(),
                Locations = new Collection<Location>(),
                Trips = new Collection<Trip>()
                {
                    new Trip()
                    {
                        TripId = 0,
                        Name = "Team Building",
                        Description = "Trip from Shanghai To Chongqing"
                    },
                    new Trip()
                    {
                        TripId = 0,
                        Name = "Team Building",
                        Description = "Trip from Chongqing To Shanghai"
                    }
                }
            },
            new Person
            {
                PersonId = 6,
                FirstName = "u4",
                FavoriteFeature = Feature.Feature4,
                Emails = new Collection<string>
                {
                    "u4@trippin.com",
                    "u4@odata.org"
                },
                HomeAddress = new Location(),
                Locations = new Collection<Location>
                {
                    new Location { Address = "a4" },
                    new Location { Address = "b4" }
                },
                Features = new Collection<Feature>
                {
                    Feature.Feature4,
                    Feature.Feature1
                },
                Trips = new Collection<Trip>()
            },
            new Person
            {
                PersonId = 7,
                FirstName = "u4",
                FavoriteFeature = Feature.Feature4,
                HomeAddress = new Location()
            }
        };

        static TrippinApi()
        {
            people[0].Friends = new Collection<Person> { people[1], people[2] };
            people[1].Friends = new Collection<Person> { people[2], people[3] };
            people[2].Friends = new Collection<Person> { people[3], people[0] };
            people[3].Friends = new Collection<Person> { people[0], people[1] };
            people[4].Friends = new Collection<Person>();
            people[5].Friends = new Collection<Person>();
            people[6].Friends = new Collection<Person>();

            people[5].BestFriend = people[4];
        }

        public IQueryable<Person> People
        {
            get { return people.AsQueryable(); }
        }

        public IQueryable<Person> NewComePeople
        {
            get { return this.GetQueryableSource<Person>("People").Where(p => p.PersonId >= 2); }
        }

        protected override IServiceCollection ConfigureApi(IServiceCollection services)
        {
            services.AddService<IModelBuilder>((sp, next) => new ModelBuilder());
            return base.ConfigureApi(services);
        }

        private class ModelBuilder : IModelBuilder
        {
            public Task<IEdmModel> GetModelAsync(ModelContext context, CancellationToken cancellationToken)
            {
                var services = new ODataConventionModelBuilder();
                services.EntityType<Person>();
                return Task.FromResult(services.GetEdmModel());
            }
        }
    }
}