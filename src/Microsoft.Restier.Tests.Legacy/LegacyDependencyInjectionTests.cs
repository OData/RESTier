// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CloudNimble.Breakdance.Assemblies;
using CloudNimble.Breakdance.Restier;
using FluentAssertions;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.Legacy
{
    [TestClass]
    public class LegacyDependencyInjectionTests
    {

        #region Tests

        [TestMethod]
        public async Task RestierRC2_VerifyContainerContents()
        {
            var provider = await RestierTestHelpers.GetTestableInjectionContainer<LegacyLibraryApi, LibraryContext>();
            var result = DependencyInjectionTestHelpers.GetContainerContentsLog(provider);
            result.Should().NotBeNullOrEmpty();

            var baseline = File.ReadAllText("..//..//..//..//Microsoft.Restier.Tests.AspNet//Baselines/RC2-LibraryApi-ServiceProvider.txt");
            result.Should().Be(baseline);
        }

        [TestMethod]
        public async Task RestierRC2_VerifyModelBuilderInnerHandlers()
        {
            var modelBuilder = await RestierTestHelpers.GetTestableInjectedService<LegacyLibraryApi, LibraryContext, IModelBuilder>();
            modelBuilder.Should().NotBeNull();

            var children = GetModelBuilderChildren(modelBuilder);
            children.Should().NotBeNullOrEmpty();

            var result = string.Join(Environment.NewLine, children);
            result.Should().NotBeNullOrWhiteSpace();

            var baseline = File.ReadAllText("..//..//..//..//Microsoft.Restier.Tests.AspNet//Baselines/RC2-ModelBuilder-InnerHandlers.txt");
            result.Should().Be(baseline);
        }

        #endregion

        #region Manifest Generators

        //[DataRow("..//..//..//..//Microsoft.Restier.Tests.Legacy//")]
        //[DataTestMethod]
        [BreakdanceManifestGenerator]
        public async Task ContainerContents_WriteOutput(string projectPath)
        {
            var provider = await RestierTestHelpers.GetTestableInjectionContainer<LegacyLibraryApi, LibraryContext>();
            var result = DependencyInjectionTestHelpers.GetContainerContentsLog(provider);
            var fullPath = Path.Combine(projectPath, "..//Microsoft.Restier.Tests.AspNet//Baselines//RC2-LibraryApi-ServiceProvider.txt");
            Console.WriteLine(fullPath);

            if (!Directory.Exists(Path.GetDirectoryName(fullPath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            }
            File.WriteAllText(fullPath, result);
            Console.WriteLine($"File exists: {File.Exists(fullPath)}");
        }

        //[DataRow("..//..//..//..//Microsoft.Restier.Tests.Legacy//")]
        //[DataTestMethod]
        [BreakdanceManifestGenerator]
        public async Task IModelBuilder_LogChildren(string projectPath)
        {
            var modelBuilder = await RestierTestHelpers.GetTestableInjectedService<LegacyLibraryApi, LibraryContext, IModelBuilder>();
            var result = GetModelBuilderChildren(modelBuilder);

            var fullPath = Path.Combine(projectPath, "..//Microsoft.Restier.Tests.AspNet//Baselines//RC2-ModelBuilder-InnerHandlers.txt");
            Console.WriteLine(fullPath);

            if (!Directory.Exists(Path.GetDirectoryName(fullPath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            }
            File.WriteAllText(fullPath, string.Join(Environment.NewLine, result));
            Console.WriteLine($"File exists: {File.Exists(fullPath)}");
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// 
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        private IModelBuilder GetInnerBuilder(object builder)
        {
            return (IModelBuilder)builder.GetPropertyValue("InnerHandler", false) ?? (IModelBuilder)builder.GetPropertyValue("InnerModelBuilder", false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="root"></param>
        /// <returns></returns>
        private List<string> GetModelBuilderChildren(IModelBuilder root)
        {
            var innerBuilders = new List<string>
            {
                root.GetType().FullName
            };
            var builder = GetInnerBuilder(root);
            do
            {
                innerBuilders.Add(builder.GetType().FullName);
                builder = GetInnerBuilder(builder);
            }
            while (builder is not null);
            return innerBuilders;
        }

        #endregion

    }

}
