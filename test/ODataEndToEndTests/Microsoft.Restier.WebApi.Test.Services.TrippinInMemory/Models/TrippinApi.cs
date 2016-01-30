using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.OData.Builder;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;

namespace Microsoft.Restier.WebApi.Test.Services.TrippinInMemory
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
            }
        };

        static TrippinApi()
        {
            people[0].Friends = new Collection<Person> { people[1], people[2] };
            people[1].Friends = new Collection<Person> { people[2], people[3] };
            people[2].Friends = new Collection<Person> { people[3], people[0] };
            people[3].Friends = new Collection<Person> { people[0], people[1] };
        }

        public IQueryable<Person> People
        {
            get { return people.AsQueryable(); }
        }

        public IQueryable<Person> NewComePeople
        {
            get { return this.Source<Person>("People").Where(p => p.PersonId >= 2); }
        }

        protected override ApiBuilder ConfigureApiBuilder(ApiBuilder builder)
        {
            return base.ConfigureApiBuilder(builder)
                .AddHookHandler<IModelBuilder>(new ModelBuilder());
        }

        private class ModelBuilder : IModelBuilder
        {
            public Task<IEdmModel> GetModelAsync(InvocationContext context, CancellationToken cancellationToken)
            {
                var builder = new ODataConventionModelBuilder();
                builder.EntityType<Person>();
                return Task.FromResult(builder.GetEdmModel());
            }
        }
    }
}