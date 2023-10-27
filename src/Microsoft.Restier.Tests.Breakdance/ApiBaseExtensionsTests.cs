// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

#if NET6_0_OR_GREATER

using CloudNimble.Breakdance.Assemblies;
using FluentAssertions;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Microsoft.Restier.Tests.Shared.Scenarios.Marvel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.Restier.Tests.Breakdance
{

    /// <summary>
    /// 
    /// </summary>
    [TestClass]
    public class ApiBaseExtensionsTests : TestHarnessBase
    {

        private const string baselinesPath = "..//..//..//..//Microsoft.Restier.Tests.AspNet//Baselines";

        /// <summary>
        /// 
        /// </summary>
        [TestMethod]
        public void LibraryApi_VisibilityMatrix()
        {
            var baseline = File.ReadAllText(Path.Combine(baselinesPath, "LibraryApi-ApiSurface.txt"));
            baseline.Should().NotBeNullOrWhiteSpace();

            var matrix = GetTestBaseInstance<LibraryApi>().GetApiInstance().GenerateVisibilityMatrix();
            matrix.Should().NotBeNullOrWhiteSpace();

            TestContext.WriteLine($"Old Report: {baseline}");
            TestContext.WriteLine($"New Report: {matrix}");

            matrix.Should().Be(baseline);
        }

        /// <summary>
        /// 
        /// </summary>
        [TestMethod]
        public void LibraryApi_VisibilityMatrix_Markdown()
        {
            var baseline = File.ReadAllText(Path.Combine(baselinesPath, "LibraryApi-ApiSurface.md"));
            baseline.Should().NotBeNullOrWhiteSpace();

            var matrix = GetTestBaseInstance<LibraryApi>().GetApiInstance().GenerateVisibilityMatrix(true);
            matrix.Should().NotBeNullOrWhiteSpace();

            TestContext.WriteLine($"Old Report: {baseline}");
            TestContext.WriteLine($"New Report: {matrix}");

            matrix.Should().Be(baseline);
        }

        /// <summary>
        /// 
        /// </summary>
        [TestMethod]
        public void MarvelApi_VisibilityMatrix()
        {
            var baseline = File.ReadAllText(Path.Combine(baselinesPath, "MarvelApi-ApiSurface.txt"));
            baseline.Should().NotBeNullOrWhiteSpace();

            var matrix = GetTestBaseInstance<MarvelApi>().GetApiInstance().GenerateVisibilityMatrix();
            matrix.Should().NotBeNullOrWhiteSpace();

            TestContext.WriteLine($"Old Report: {baseline}");
            TestContext.WriteLine($"New Report: {matrix}");

            matrix.Should().Be(baseline);
        }

        /// <summary>
        /// 
        /// </summary>
        [TestMethod]
        public void MarvelApi_VisibilityMatrix_Markdown()
        {
            var baseline = File.ReadAllText(Path.Combine(baselinesPath, "MarvelApi-ApiSurface.md"));
            baseline.Should().NotBeNullOrWhiteSpace();

            var matrix = GetTestBaseInstance<MarvelApi>().GetApiInstance().GenerateVisibilityMatrix(true);
            matrix.Should().NotBeNullOrWhiteSpace();

            TestContext.WriteLine($"Old Report: {baseline}");
            TestContext.WriteLine($"New Report: {matrix}");

            matrix.Should().Be(baseline);
        }

        /// <summary>
        /// 
        /// </summary>
        [TestMethod]
        public void StoreApi_VisibilityMatrix()
        {
            var baseline = File.ReadAllText(Path.Combine(baselinesPath, "StoreApi-ApiSurface.txt"));
            baseline.Should().NotBeNullOrWhiteSpace();

            var matrix = GetTestBaseInstance<StoreApi>().GetApiInstance().GenerateVisibilityMatrix();
            matrix.Should().NotBeNullOrWhiteSpace();

            TestContext.WriteLine($"Old Report: {baseline}");
            TestContext.WriteLine($"New Report: {matrix}");

            matrix.Should().Be(baseline);
        }

        /// <summary>
        /// 
        /// </summary>
        [TestMethod]
        public void StoreApi_VisibilityMatrix_Markdown()
        {
            var baseline = File.ReadAllText(Path.Combine(baselinesPath, "StoreApi-ApiSurface.md"));
            baseline.Should().NotBeNullOrWhiteSpace();

            var matrix = GetTestBaseInstance<StoreApi>().GetApiInstance().GenerateVisibilityMatrix(true);
            matrix.Should().NotBeNullOrWhiteSpace();

            TestContext.WriteLine($"Old Report: {baseline}");
            TestContext.WriteLine($"New Report: {matrix}");

            matrix.Should().Be(baseline);
        }

        #region Manifest Generators

        //[DataRow(baselinesPath)]
        //[DataTestMethod]
        [BreakdanceManifestGenerator]
        public void LibraryApi_ApiSurface_WriteOutput(string projectPath)
        {
            GetTestBaseInstance<LibraryApi>().GetApiInstance().WriteCurrentVisibilityMatrix(projectPath);
            GetTestBaseInstance<LibraryApi>().GetApiInstance().WriteCurrentVisibilityMatrix(projectPath, markdown: true);
        }

        //[DataRow(baselinesPath)]
        //[DataTestMethod]
        [BreakdanceManifestGenerator]
        public void MarvelApi_ApiSurface_WriteOutput(string projectPath)
        {
            GetTestBaseInstance<MarvelApi>().GetApiInstance().WriteCurrentVisibilityMatrix(projectPath);
            GetTestBaseInstance<MarvelApi>().GetApiInstance().WriteCurrentVisibilityMatrix(projectPath, markdown: true);
        }

        //[DataRow(baselinesPath)]
        //[DataTestMethod]
        [BreakdanceManifestGenerator]
        public void StoreApi_ApiSurface_WriteOutput(string projectPath)
        {
            GetTestBaseInstance<StoreApi>().GetApiInstance().WriteCurrentVisibilityMatrix(projectPath);
            GetTestBaseInstance<StoreApi>().GetApiInstance().WriteCurrentVisibilityMatrix(projectPath, markdown: true);
        }

        ////[DataRow("..//..//..//..//Microsoft.Restier.Tests.Legacy//")]
        ////[DataTestMethod]
        //[BreakdanceManifestGenerator]
        //public async Task IModelBuilder_LogChildren(string projectPath)
        //{
        //    //var modelBuilder = await RestierTestHelpers.GetTestableInjectedService<LegacyLibraryApi, LibraryContext, IModelBuilder>();
        //    //var result = GetModelBuilderChildren(modelBuilder);

        //    //var fullPath = Path.Combine(projectPath, "..//Microsoft.Restier.Tests.AspNet//Baselines//RC2-ModelBuilder-InnerHandlers.txt");
        //    //Console.WriteLine(fullPath);

        //    //if (!Directory.Exists(Path.GetDirectoryName(fullPath)))
        //    //{
        //    //    Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
        //    //}
        //    //File.WriteAllText(fullPath, string.Join(Environment.NewLine, result));
        //    //Console.WriteLine($"File exists: {File.Exists(fullPath)}");
        //}

        #endregion



    }

}

#endif