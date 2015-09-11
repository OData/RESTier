using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.OData.Builder;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;

namespace Microsoft.Restier.WebApi.Test.Services.TrippinInMemory
{
    public class TrippinDomain : DomainBase
    {
        private static readonly List<Person> people = new List<Person> { new Person { PersonId = 1, UserName = "u2" } };

        protected IQueryable<Person> People
        {
            get { return people.AsQueryable(); }
        }

        protected override DomainConfiguration CreateDomainConfiguration()
        {
            return base.CreateDomainConfiguration()
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