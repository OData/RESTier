// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Tests.Shared.Scenarios.Library.EF6;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests.EF6;

[Collection("LibraryApiEF6")]
public class ActionTests(ITestOutputHelper outputHelper) : ActionTests<LibraryApi, LibraryContext>(outputHelper)
{
    protected override Action<IServiceCollection> ConfigureServices
        => services => services.AddEntityFrameworkServices<LibraryContext>();

    protected override async Task RestoreOrphanBookAsync()
    {
        var context = await RestierTestHelpers.GetTestableInjectedService<LibraryApi, LibraryContext>(
            serviceCollection: ConfigureServices);

        var orphanBookId = new Guid("2d760f15-974d-4556-8cdf-d610128b537e");
        var orphan = context.Books.FirstOrDefault(b => b.Id == orphanBookId);
        if (orphan is not null && orphan.PublisherId is not null)
        {
            orphan.PublisherId = null;
            await context.SaveChangesAsync();
        }
    }
}
