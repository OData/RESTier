// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.IO;
using System.Threading.Tasks;
using CloudNimble.Breakdance.Assemblies;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Microsoft.Restier.Tests.Shared.Scenarios.Marvel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#if NET6_0_OR_GREATER
namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests
#else
namespace Microsoft.Restier.Tests.AspNet.FeatureTests
#endif
{

#if NET6_0_OR_GREATER

    [TestClass]
    [TestCategory("Endpoint Routing")]
    public class MetadataTests_EndpointRouting : MetadataTests
    {
        public MetadataTests_EndpointRouting() : base(true)
        {
        }
    }

    [TestClass]
    [TestCategory("Legacy Routing")]
    public class MetadataTests_LegacyRouting : MetadataTests
    {
        public MetadataTests_LegacyRouting() : base(false)
        {
        }
    }

    /// <summary>
    /// 
    /// </summary>
    [TestClass]
    public abstract class MetadataTests : RestierTestBase<LibraryApi>
    {

        public MetadataTests(bool useEndpointRouting) : base(useEndpointRouting)
        {
            //AddRestierAction = builder =>
            //{
            //    builder.AddRestierApi<LibraryApi>(services => services.AddEntityFrameworkServices<LibraryContext>());
            //};
            //MapRestierAction = routeBuilder =>
            //{
            //    routeBuilder.MapApiRoute<LibraryApi>(WebApiConstants.RouteName, WebApiConstants.RoutePrefix, false);
            //};
        }

        //[TestInitialize]
        //public void ClaimsTestSetup() => TestSetup();

#else

    /// <summary>
    /// 
    /// </summary>
    [TestClass]
    public class MetadataTests : RestierTestBase
    {

#endif

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
             * */
            var fileName = $"{Path.Combine(relativePath, baselineFolder)}{typeof(LibraryApi).Name}-ApiMetadata.txt";
            File.Exists(fileName).Should().BeTrue();

            var oldReport = File.ReadAllText(fileName);
            var newReport = await RestierTestHelpers.GetApiMetadataAsync<LibraryApi>(serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>(),
                useEndpointRouting: UseEndpointRouting);

            TestContext.WriteLine($"Old Report: {oldReport}");
            TestContext.WriteLine($"New Report: {newReport}");

            oldReport.Should().BeEquivalentTo(newReport.ToString());
        }

        //[DataRow(relativePath)]
        //[DataTestMethod]
        [BreakdanceManifestGenerator]
        public async Task LibraryApi_SaveMetadataDocument(string projectPath)
        {
            await RestierTestHelpers.WriteCurrentApiMetadata<LibraryApi>(Path.Combine(projectPath, baselineFolder),
                serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>(), useEndpointRouting: UseEndpointRouting);
            File.Exists($"{Path.Combine(projectPath, baselineFolder)}{typeof(LibraryApi).Name}-ApiMetadata.txt").Should().BeTrue();
        }

        #endregion

        #region MarvelApi

        [TestMethod]
        public async Task MarvelApi_CompareCurrentApiMetadataToPriorRun()
        {
            var fileName = $"{Path.Combine(relativePath, baselineFolder)}{typeof(MarvelApi).Name}-ApiMetadata.txt";
            File.Exists(fileName).Should().BeTrue();

            var oldReport = File.ReadAllText(fileName);
            var newReport = await RestierTestHelpers.GetApiMetadataAsync<MarvelApi>(serviceCollection: (services) => services.AddEntityFrameworkServices<MarvelContext>(),
                useEndpointRouting: UseEndpointRouting);

            TestContext.WriteLine($"Old Report: {oldReport}");
            TestContext.WriteLine($"New Report: {newReport}");

            oldReport.Should().BeEquivalentTo(newReport.ToString());
        }

        //[DataRow(relativePath)]
        //[DataTestMethod]
        [BreakdanceManifestGenerator]
        public async Task MarvelApi_SaveMetadataDocument(string projectPath)
        {
            await RestierTestHelpers.WriteCurrentApiMetadata<MarvelApi>(Path.Combine(projectPath, baselineFolder),
                serviceCollection: (services) => services.AddEntityFrameworkServices<MarvelContext>(), useEndpointRouting: UseEndpointRouting);
            File.Exists($"{Path.Combine(projectPath, baselineFolder)}{typeof(MarvelApi).Name}-ApiMetadata.txt").Should().BeTrue();
        }

        #endregion

        #region StoreApi

        [TestMethod]
        public async Task StoreApi_CompareCurrentApiMetadataToPriorRun()
        {
            var fileName = $"{Path.Combine(relativePath, baselineFolder)}{typeof(StoreApi).Name}-ApiMetadata.txt";
            File.Exists(fileName).Should().BeTrue();

            var oldReport = File.ReadAllText(fileName);
            var newReport = await RestierTestHelpers.GetApiMetadataAsync<StoreApi>(serviceCollection: (services) => services.AddTestStoreApiServices(),
                useEndpointRouting: UseEndpointRouting);

            TestContext.WriteLine($"Old Report: {oldReport}");
            TestContext.WriteLine($"New Report: {newReport}");

            oldReport.Should().BeEquivalentTo(newReport.ToString());
        }

        //[DataRow(relativePath)]
        //[DataTestMethod]
        [BreakdanceManifestGenerator]
        public async Task StoreApi_SaveMetadataDocument(string projectPath)
        {
            await RestierTestHelpers.WriteCurrentApiMetadata<StoreApi>(Path.Combine(projectPath, baselineFolder), serviceCollection: (services) => services.AddTestStoreApiServices(),
                useEndpointRouting: UseEndpointRouting);
            File.Exists($"{Path.Combine(projectPath, baselineFolder)}{typeof(StoreApi).Name}-ApiMetadata.txt").Should().BeTrue();
        }

        #endregion


    }

}