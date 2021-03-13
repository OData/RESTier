using System.Data.Entity;
using System.IO;
using System.Threading.Tasks;
using CloudNimble.Breakdance.Restier;
using FluentAssertions;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.AspNet.FeatureTests
{

    [TestClass]
    public class MetadataTests : RestierTestBase
    {

        #region Private Members

        private const string relativePath = "..//..//..//Baselines//";

        #endregion

        #region LibraryApi

        [Ignore]
        [TestMethod]
        public async Task LibraryApi_SaveMetadataDocument()
        {
            await RestierTestHelpers.WriteCurrentApiMetadata<LibraryApi, LibraryContext>(relativePath);
            File.Exists($"{relativePath}{typeof(LibraryApi).Name}-ApiMetadata.txt").Should().BeTrue();
        }

        [Ignore]
        [TestMethod]
        public async Task LibraryApi_SaveVisibilityMatrix()
        {
            var api = await RestierTestHelpers.GetTestableApiInstance<LibraryApi, LibraryContext>();
            await api.WriteCurrentVisibilityMatrix(relativePath);

            File.Exists($"{relativePath}{api.GetType().Name}-ApiSurface.txt").Should().BeTrue();
        }

        [TestMethod]
        public async Task LibraryApi_CompareCurrentApiMetadataToPriorRun()
        {
            var fileName = $"{relativePath}{typeof(LibraryApi).Name}-ApiMetadata.txt";
            File.Exists(fileName).Should().BeTrue();

            var oldReport = File.ReadAllText(fileName);
            var newReport = await RestierTestHelpers.GetApiMetadata<LibraryApi, LibraryContext>();
            oldReport.Should().BeEquivalentTo(newReport.ToString());
        }

        [TestMethod]
        public async Task LibraryApi_CompareCurrentVisibilityMatrixToPriorRun()
        {
            var api = await RestierTestHelpers.GetTestableApiInstance<LibraryApi, LibraryContext>();
            var fileName = $"{relativePath}{api.GetType().Name}-ApiSurface.txt";

            File.Exists(fileName).Should().BeTrue();
            var oldReport = File.ReadAllText(fileName);
            var newReport = await api.GenerateVisibilityMatrix();
            oldReport.Should().BeEquivalentTo(newReport);
        }

        #endregion

        #region StoreApi

        [Ignore]
        [TestMethod]
        public async Task StoreApi_SaveMetadataDocument()
        {
            await RestierTestHelpers.WriteCurrentApiMetadata<StoreApi, DbContext>(relativePath);
            File.Exists($"{relativePath}{typeof(StoreApi).Name}-ApiMetadata.txt").Should().BeTrue();
        }

        [Ignore]
        [TestMethod]
        public async Task StoreApi_SaveVisibilityMatrix()
        {
            var api = await RestierTestHelpers.GetTestableApiInstance<StoreApi, DbContext>(serviceCollection: (services) => { services.AddTestStoreApiServices(); });
            await api.WriteCurrentVisibilityMatrix(relativePath);

            File.Exists($"{relativePath}{api.GetType().Name}-ApiSurface.txt").Should().BeTrue();
        }

        [TestMethod]
        public async Task StoreApi_CompareCurrentApiMetadataToPriorRun()
        {
            var fileName = $"{relativePath}{typeof(StoreApi).Name}-ApiMetadata.txt";
            File.Exists(fileName).Should().BeTrue();

            var oldReport = File.ReadAllText(fileName);
            var newReport = await RestierTestHelpers.GetApiMetadata<StoreApi, DbContext>(serviceCollection: (services) => { services.AddTestStoreApiServices(); });
            oldReport.Should().BeEquivalentTo(newReport.ToString());
        }

        [TestMethod]
        public async Task StoreApi_CompareCurrentVisibilityMatrixToPriorRun()
        {
            var api = await RestierTestHelpers.GetTestableApiInstance<StoreApi, DbContext>(serviceCollection: (services) => { services.AddTestStoreApiServices(); });
            var fileName = $"{relativePath}{api.GetType().Name}-ApiSurface.txt";

            File.Exists(fileName).Should().BeTrue();
            var oldReport = File.ReadAllText(fileName);
            var newReport = await api.GenerateVisibilityMatrix();
            oldReport.Should().BeEquivalentTo(newReport);
        }

        #endregion


    }

}