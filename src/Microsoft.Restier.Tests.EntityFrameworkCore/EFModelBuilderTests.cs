// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.EntityFrameworkCore;
using Microsoft.Restier.Tests.EntityFrameworkCore.Scenarios.IncorrectLibrary;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

#if NET6_0_OR_GREATER
using Microsoft.Restier.Tests.EntityFrameworkCore.Scenarios.Views;
#endif

namespace Microsoft.Restier.Tests.EntityFrameworkCore
{

    [TestClass]
    public class EFModelBuilderTests
    {

        /// <summary>
        /// Tests that mapping a complex type to a DbSet in the model causes an exception.
        /// </summary>
        /// <remarks>This is not supported because the EFModelBuilder requires that a primary key is defined for each type in the model.</remarks>
        [TestMethod]
        public async Task DbSetOnComplexType_Should_ThrowException()
        {
            var provider = await RestierTestHelpers.GetTestableInjectionContainer<IncorrectLibraryApi>(serviceCollection: (services) => services.AddEFCoreProviderServices<IncorrectLibraryContext>());
            var api = provider.GetTestableApiInstance<IncorrectLibraryApi>();
            Action getModelAction = () =>  new EFModelBuilder().GetModel(new ModelContext(api));
            getModelAction.Should().Throw<EdmModelValidationException>().Where(c => c.Message.Contains("Address") && c.Message.Contains("Universe"));
        }

#if NET6_0_OR_GREATER

        /// <summary>
        /// Tests that APIs that try to map Views to DbSets throws an InvalidOperationException, per https://docs.microsoft.com/en-us/odata/webapi/abstract-entity-types.
        /// </summary>
        /// <remarks>This is not supported because the EFModelBuilder requires that a primary key is defined for each type in the model.</remarks>
        [TestMethod]
        public void EFModelBuilder_Should_HandleViews()
        {
            var getModelAction = async () =>
            {
                _ = await RestierTestHelpers.GetApiMetadataAsync<LibraryWithViewsApi>(serviceCollection: (services) => services.AddEFCoreProviderServices<LibraryWithViewsContext>());
            };
            getModelAction.Should().ThrowAsync<InvalidOperationException>().Where(c => c.Message.Contains("[Keyless]"));
        }

#endif

    }

}
