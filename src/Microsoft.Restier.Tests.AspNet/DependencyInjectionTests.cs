﻿using System;
using System.IO;
using System.Threading.Tasks;
using CloudNimble.Breakdance.Assemblies;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Startup;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.Core
{

    /// <summary>
    /// Tests methods of the Core ServiceCollectionExtensions.
    /// </summary>
    [TestClass]
    public class DependencyInjectionTests : RestierTestBase
    {

        [TestMethod]
        public void RestierContainerBuilder_Registered_ShouldHaveServices(string path)
        {
            var container = GetContainerBuilder();
            container.Services.Should().HaveCount(48);
        }

        [TestMethod]
        public async Task DI_CompareCurrentVersion_ToRC2()
        {
            var provider = await RestierTestHelpers.GetTestableInjectionContainer<LibraryApi, LibraryContext>();
            var result = DependencyInjectionTestHelpers.GetContainerContentsLog(provider);
            result.Should().NotBeNullOrEmpty();

            var baseline = File.ReadAllText("..//..//..//..//Microsoft.Restier.Tests.AspNet//Baselines/RC2-LibraryApi-ServiceProvider.txt");
            result.Should().Be(baseline);
        }

        [TestMethod]
        public async Task DI_VerifyModelBuilderInnerHandlers_ToRC2()
        {
            var names = await RestierTestHelpers.GetModelBuilderHierarchy<LibraryApi, LibraryContext>();
            names.Should().NotBeNull();

            var result = string.Join(Environment.NewLine, names);
            result.Should().NotBeNullOrWhiteSpace();

            var baseline = File.ReadAllText("..//..//..//..//Microsoft.Restier.Tests.AspNet//Baselines/RC2-ModelBuilder-InnerHandlers.txt");
            result.Should().Be(baseline);
        }

        [TestMethod]
        [BreakdanceManifestGenerator]
        public async Task ContainerContents_WriteOutput(/*string projectPath*/)
        {
            var projectPath = "..//..//..//";
            var provider = await RestierTestHelpers.GetTestableInjectionContainer<LibraryApi, LibraryContext>();
            var result = DependencyInjectionTestHelpers.GetContainerContentsLog(provider);
            var fullPath = Path.Combine(projectPath, "Baselines//RC6-LibraryApi-ServiceProvider.txt");
            Console.WriteLine(fullPath);

            if (!Directory.Exists(Path.GetDirectoryName(fullPath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            }
            File.WriteAllText(fullPath, result);
            Console.WriteLine($"File exists: {File.Exists(fullPath)}");
        }

        [TestMethod]
        [BreakdanceManifestGenerator]
        public async Task IModelBuilder_LogChildren(/*string projectPath*/)
        {
            var projectPath = "..//..//..//";

            var result = await RestierTestHelpers.GetModelBuilderHierarchy<LibraryApi, LibraryContext>();

            var fullPath = Path.Combine(projectPath, "Baselines//RC6-ModelBuilder-InnerHandlers.txt");
            Console.WriteLine(fullPath);

            if (!Directory.Exists(Path.GetDirectoryName(fullPath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            }
            File.WriteAllText(fullPath, string.Join(Environment.NewLine, result));
            Console.WriteLine($"File exists: {File.Exists(fullPath)}");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private RestierContainerBuilder GetContainerBuilder()
        {
            var container = new RestierContainerBuilder();
            container.Services
                .AddRestierCoreServices()
                .AddRestierConventionBasedServices(typeof(LibraryApi))
                .AddRestierDefaultServices();
            return container;
        }

    }

}