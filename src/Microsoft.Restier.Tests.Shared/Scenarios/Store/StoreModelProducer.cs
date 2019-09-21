using System.Threading;
using System.Threading.Tasks;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core.Model;

namespace Microsoft.Restier.Tests.Shared
{
    internal class StoreModelProducer : IModelBuilder
    {
        private readonly EdmModel model;

        public StoreModelProducer(EdmModel model)
        {
            this.model = model;
        }

        public Task<IEdmModel> GetModelAsync(ModelContext context, CancellationToken cancellationToken)
        {
            return Task.FromResult<IEdmModel>(model);
        }
    }
}
