using Microsoft.EntityFrameworkCore;

namespace Microsoft.Restier.Tests.Shared.EntityFrameworkCore
{

    public interface IDatabaseInitializer
    {

        public void Seed(DbContext context);

    }

}
