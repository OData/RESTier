using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Submit;
using Microsoft.Restier.EntityFrameworkCore;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Restier.Tests.EntityFrameworkCore
{

    [TestClass]
    public class EFModelBuilderTests
    {

        /// <summary>
        /// Tests that the IsDbSetMapped extension works as expected
        /// </summary>
        [TestMethod]
        public void IsDbSetMapped_CanFind_MappedDbSets()
        {
            using var context = new LibraryContext(new DbContextOptions<LibraryContext> { });
            context.IsDbSetMapped(typeof(Address)).Should().BeFalse();
            using var incorrectContext = new IncorrectLibraryContext(new DbContextOptions<LibraryContext> { });
            incorrectContext.IsDbSetMapped(typeof(Address)).Should().BeTrue();
        }

        /// <summary>
        /// Tests that mapping a complex type to a DbSet in the model causes an exception.
        /// </summary>
        /// <remarks>This is not supported because the EFModelBuilder requires that a primary key is defined for each type in the model.</remarks>
        [TestMethod]
        public async Task DbSetOnComplexType_Should_ThrowExceptionAsync()
        {
            var provider = await RestierTestHelpers.GetTestableInjectionContainer<LibraryApi>(serviceCollection: (services) => services.AddEntityFrameworkServices<IncorrectLibraryContext>());
            var api = provider.GetTestableApiInstance<LibraryApi>();
            Action act = () =>  new EFModelBuilder().GetModel(new ModelContext(api));
            act.Should().Throw<ChangeSetValidationException>().Where(c => c.Message.Contains("Address") && c.Message.Contains("Universe"));
        }

    }


    /// <summary>
    /// Class that extends the existing LibraryContext incorrectly by adding <see cref="DbSet"/> mappings for the complex types <see cref="Address"/> and <see cref="Universe"/>.
    /// </summary>
    public class IncorrectLibraryContext : LibraryContext
    {

        public DbSet<Address> Addresses { get; set; }

        public DbSet<Universe> Universes { get; set; }

        public IncorrectLibraryContext(DbContextOptions<LibraryContext> options) : base(options)
        {
        }

    }

}
