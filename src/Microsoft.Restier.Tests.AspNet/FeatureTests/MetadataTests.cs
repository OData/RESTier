using System.IO;
using System.Threading.Tasks;
using CloudNimble.Breakdance.Assemblies;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#if NETCOREAPP3_1 || NETCOREAPP3_1_OR_GREATER
namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests
#else
namespace Microsoft.Restier.Tests.AspNet.FeatureTests
#endif
{

    [TestClass]
    public class MetadataTests : RestierTestBase
#if NETCOREAPP3_1_OR_GREATER
        <LibraryApi>
#endif
    {

        #region Private Members

#if EFCore
        private const string relativePath = "..//..//..//..//Microsoft.Restier.Tests.AspNetCore//";
#endif
#if EF6
        private const string relativePath = "..//..//..//..//Microsoft.Restier.Tests.AspNet//";
#endif
        private const string baselineFolder = "Baselines//";

        #endregion

        #region LibraryApi

        [TestMethod]
        public async Task LibraryApi_CompareCurrentApiMetadataToPriorRun()
        {
            /* JHC Note:
             * in Restier.Tests.AspNet, this test fails because we haven't generated an updated ApiMetadata after some changes
             * 
             * */
            var fileName = $"{Path.Combine(relativePath, baselineFolder)}{typeof(LibraryApi).Name}-ApiMetadata.txt";
            File.Exists(fileName).Should().BeTrue();

            var oldReport = File.ReadAllText(fileName);
            var newReport = await RestierTestHelpers.GetApiMetadataAsync<LibraryApi>(serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>());

            TestContext.WriteLine($"Old Report: {oldReport}");
            TestContext.WriteLine($"New Report: {newReport}");

            oldReport.Should().BeEquivalentTo(newReport.ToString());
        }

        [TestMethod]
        public async Task LibraryApi_CompareCurrentVisibilityMatrixToPriorRun()
        {
            /* JHC Note:
             * in Restier.Tests.AspNet, this test fails because we haven't generated an updated ApiSurface after making some changes to the data model
             * 
             * */
            var api = await RestierTestHelpers.GetTestableApiInstance<LibraryApi>(serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>());
            var fileName = $"{Path.Combine(relativePath, baselineFolder)}{api.GetType().Name}-ApiSurface.txt";

            File.Exists(fileName).Should().BeTrue();
            var oldReport = File.ReadAllText(fileName);
            var newReport = api.GenerateVisibilityMatrix();

            TestContext.WriteLine($"Old Report: {oldReport}");
            TestContext.WriteLine($"New Report: {newReport}");

            oldReport.Should().BeEquivalentTo(newReport);
        }

        //[DataRow(relativePath)]
        //[DataTestMethod]
        [BreakdanceManifestGenerator]
        public async Task LibraryApi_SaveMetadataDocument(string projectPath)
        {
            await RestierTestHelpers.WriteCurrentApiMetadata<LibraryApi>(Path.Combine(projectPath, baselineFolder), serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>());
            File.Exists($"{Path.Combine(projectPath, baselineFolder)}{typeof(LibraryApi).Name}-ApiMetadata.txt").Should().BeTrue();
        }

        //[DataRow(relativePath)]
        //[DataTestMethod]
        [BreakdanceManifestGenerator]
        public async Task LibraryApi_SaveVisibilityMatrix(string projectPath)
        {
            var api = await RestierTestHelpers.GetTestableApiInstance<LibraryApi>(serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>());
            api.WriteCurrentVisibilityMatrix(Path.Combine(projectPath, baselineFolder));

            File.Exists($"{Path.Combine(projectPath, baselineFolder)}{api.GetType().Name}-ApiSurface.txt").Should().BeTrue();
        }

        #endregion

        #region StoreApi

        [TestMethod]
        public async Task StoreApi_CompareCurrentApiMetadataToPriorRun()
        {
            /* JHC Note:
             * in Restier.Tests.AspNet, this test fails because we haven't generated an updated ApiMetadata after some changes
             * 
             * */
            var fileName = $"{Path.Combine(relativePath, baselineFolder)}{typeof(StoreApi).Name}-ApiMetadata.txt";
            File.Exists(fileName).Should().BeTrue();

            var oldReport = File.ReadAllText(fileName);
            var newReport = await RestierTestHelpers.GetApiMetadataAsync<StoreApi>(serviceCollection: (services) => services.AddTestStoreApiServices());

            TestContext.WriteLine($"Old Report: {oldReport}");
            TestContext.WriteLine($"New Report: {newReport}");

            oldReport.Should().BeEquivalentTo(newReport.ToString());
        }

        [TestMethod]
        public async Task StoreApi_CompareCurrentVisibilityMatrixToPriorRun()
        {
            var api = await RestierTestHelpers.GetTestableApiInstance<StoreApi>(serviceCollection: (services) => services.AddTestStoreApiServices());
            var fileName = $"{Path.Combine(relativePath, baselineFolder)}{api.GetType().Name}-ApiSurface.txt";

            File.Exists(fileName).Should().BeTrue();
            var oldReport = File.ReadAllText(fileName);
            var newReport = api.GenerateVisibilityMatrix();

            TestContext.WriteLine($"Old Report: {oldReport}");
            TestContext.WriteLine($"New Report: {newReport}");

            oldReport.Should().BeEquivalentTo(newReport);
        }

        //[DataRow(relativePath)]
        //[DataTestMethod]
        [BreakdanceManifestGenerator]
        public async Task StoreApi_SaveMetadataDocument(string projectPath)
        {
            await RestierTestHelpers.WriteCurrentApiMetadata<StoreApi>(Path.Combine(projectPath, baselineFolder), serviceCollection: (services) => services.AddTestStoreApiServices());
            File.Exists($"{Path.Combine(projectPath, baselineFolder)}{typeof(StoreApi).Name}-ApiMetadata.txt").Should().BeTrue();
        }

        //[DataRow(relativePath)]
        //[DataTestMethod]
        [BreakdanceManifestGenerator]
        public async Task StoreApi_SaveVisibilityMatrix(string projectPath)
        {
            var api = await RestierTestHelpers.GetTestableApiInstance<StoreApi>(serviceCollection: (services) => services.AddTestStoreApiServices());
            api.WriteCurrentVisibilityMatrix(Path.Combine(projectPath, baselineFolder));

            File.Exists($"{Path.Combine(projectPath, baselineFolder)}{api.GetType().Name}-ApiSurface.txt").Should().BeTrue();
        }

        #endregion


    }

}