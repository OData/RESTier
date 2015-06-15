using Microsoft.Restier.Core;

namespace Microsoft.Restier.EntityFramework.Tests.Models.Library
{
    class LibraryDomain : DbDomain<LibraryContext>
    {
        internal DomainContext Context
        {
            get { return this.DomainContext; }
        }
    }
}
