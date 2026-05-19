// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.EntityFrameworkCore;
using Microsoft.Restier.Tests.EntityFrameworkCore.Scenarios.IncorrectLibrary;
using Microsoft.Restier.Tests.Shared.Scenarios.Library.EFCore;
using Microsoft.Restier.Tests.Shared.Scenarios.Library.EFCore.Views;
using Xunit;

namespace Microsoft.Restier.Tests.EntityFrameworkCore;

public class EFModelBuilderTests
{
    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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
