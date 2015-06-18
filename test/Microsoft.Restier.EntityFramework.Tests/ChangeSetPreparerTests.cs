using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Submit;
using Microsoft.Restier.EntityFramework.Submit;
using Microsoft.Restier.EntityFramework.Tests.Models.Library;
using Xunit;

namespace Microsoft.Restier.EntityFramework.Tests
{
    public class ChangeSetPreparerTests
    {
        [Fact]
        public async Task ComplexTypeUpdate()
        {
            var libraryDomain = new LibraryDomain();
            var entry = new DataModificationEntry(
                "Readers",
                "Person",
                new Dictionary<string, object> { { "Id", new Guid("53162782-EA1B-4712-AF26-8AA1D2AC0461") } },
                new Dictionary<string, object>(),
                new Dictionary<string, object> { { "Addr", new Dictionary<string, object> { { "Zip", "332" } } } });
            var changeSet = new ChangeSet(new[] { entry });
            var sc = new SubmitContext(libraryDomain.Context, changeSet);
            await ChangeSetPreparer.Instance.PrepareAsync(sc, CancellationToken.None);
            var person = entry.Entity as Person;
            Assert.NotNull(person);
            Assert.Equal("332", person.Addr.Zip);
        }
    }
}
