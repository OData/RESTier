// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.EntityFrameworkCore;
using Microsoft.Restier.Tests.EntityFrameworkCore.EFModelBuilderScenario;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

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

    }

}
