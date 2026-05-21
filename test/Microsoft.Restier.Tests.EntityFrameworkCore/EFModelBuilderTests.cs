// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Core;
using Microsoft.Restier.EntityFrameworkCore;
using Microsoft.Restier.Tests.EntityFrameworkCore.Scenarios.IncorrectLibrary;
using Microsoft.Restier.Tests.Shared.Scenarios.Library.EFCore;
using Microsoft.Restier.Tests.Shared.Scenarios.Library.EFCore.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.EntityFrameworkCore;

[TestClass]
public class EFModelBuilderTests
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public async Task DbSetOnComplexType_Should_ThrowException()
    {
        var getModelAction = async () =>
        {
            _ = await RestierTestHelpers.GetApiMetadataAsync<IncorrectLibraryApi>(
                serviceCollection: services => services.AddEFCoreProviderServices<IncorrectLibraryContext>((Action<DbContextOptionsBuilder>)null));
        };
        await getModelAction.Should().ThrowAsync<InvalidOperationException>()
            .Where(c => c.ToString().Contains("Address") && c.ToString().Contains("Universe"));
    }

    [TestMethod]
    public async Task EFModelBuilder_Should_HandleViews()
    {
        var metadata = await RestierTestHelpers.GetApiMetadataAsync<LibraryWithViewsApi>(
            serviceCollection: services => services.AddEntityFrameworkServices<LibraryContext>());

        metadata.Should().NotBeNull();
        var metadataString = metadata.ToString();

        // The keyless view appears as a ComplexType, not an EntityType.
        metadataString.Should().Contain("ComplexType Name=\"BooksByPublisher\"");
        metadataString.Should().NotContain("EntityType Name=\"BooksByPublisher\"");

        // And as an unbound FunctionImport returning a Collection of that ComplexType.
        metadataString.Should().Contain("FunctionImport Name=\"BooksByPublisher\"");
        metadataString.Should().MatchRegex("Function Name=\"BooksByPublisher\"[\\s\\S]*ReturnType[\\s\\S]*Type=\"Collection\\([^\"]*BooksByPublisher\\)\"");
    }

    [TestMethod]
    public async Task EFModelBuilder_Should_HandleMixedModel()
    {
        var metadata = await RestierTestHelpers.GetApiMetadataAsync<LibraryWithViewsApi>(
            serviceCollection: services => services.AddEntityFrameworkServices<LibraryContext>());

        var metadataString = metadata.ToString();

        // Regular entity sets coexist with the keyless view.
        metadataString.Should().Contain("EntityType Name=\"Book\"");
        metadataString.Should().Contain("EntityType Name=\"Publisher\"");
        metadataString.Should().Contain("EntitySet Name=\"Books\"");
        metadataString.Should().Contain("EntitySet Name=\"Publishers\"");

        metadataString.Should().Contain("ComplexType Name=\"BooksByPublisher\"");
        metadataString.Should().Contain("FunctionImport Name=\"BooksByPublisher\"");
    }

    [TestMethod]
    public async Task EFModelBuilder_LowerCamelCase_KeylessViewImport_MatchesEntitySetCasing()
    {
        // ODataConventionModelBuilder.EnableLowerCamelCase() lower-camel-cases *property* and
        // enum-member names — NOT container-level names. EntitySets stay PascalCase in
        // LowerCamelCase routes; keyless-view function imports should match. This pins the
        // behaviour so a future "let's also lower-camel-case the function import" tweak would
        // be a deliberate choice rather than an accidental drift.
        var response = await RestierTestHelpers.ExecuteTestRequest<LibraryWithViewsApi>(
            HttpMethod.Get,
            resource: "/$metadata",
            acceptHeader: "application/xml",
            serviceCollection: services => services.AddEntityFrameworkServices<LibraryContext>(),
            namingConvention: RestierNamingConvention.LowerCamelCase);

        response.IsSuccessStatusCode.Should().BeTrue();
        var metadataString = await response.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);

        // Convention sanity: entity-set names stay PascalCase, but properties get camelCased.
        metadataString.Should().Contain("EntitySet Name=\"Books\"");
        metadataString.Should().Contain("Property Name=\"isbn\"");

        // The keyless-view function import follows the EntitySet casing rule — PascalCase.
        metadataString.Should().Contain("FunctionImport Name=\"BooksByPublisher\"");
        metadataString.Should().NotContain("FunctionImport Name=\"booksByPublisher\"");
    }

    [TestMethod]
    public async Task GetEdmModel_ShouldBuildValidModel_ForStandardContext()
    {
        var metadata = await RestierTestHelpers.GetApiMetadataAsync<LibraryApi>(
            serviceCollection: services => services.AddEntityFrameworkServices<LibraryContext>());

        metadata.Should().NotBeNull();
        var metadataString = metadata.ToString();
        metadataString.Should().Contain("Books");
        metadataString.Should().Contain("Publishers");
        metadataString.Should().Contain("Readers");
    }
}
