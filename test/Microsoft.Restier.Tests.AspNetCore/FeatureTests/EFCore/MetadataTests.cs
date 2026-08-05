// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Tests.Shared.Scenarios.Library.EFCore;
using Microsoft.Restier.Tests.Shared.Scenarios.Marvel.EFCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests.EFCore;

[TestClass]
[DoNotParallelize]
public class MetadataTests : MetadataTests<LibraryApi, LibraryContext>
{
    protected override Action<IServiceCollection> ConfigureServices
        => services => services.AddEntityFrameworkServices<LibraryContext>();

    protected override string ProviderName => "EFCore";

    protected override string MarvelBaselinePrefix => "MarvelApi-EFCore";

    protected override async Task<XDocument> GetMarvelApiMetadataAsync()
    {
        return await RestierTestHelpers.GetApiMetadataAsync<MarvelApi>(
            serviceCollection: services => services.AddEntityFrameworkServices<MarvelContext>());
    }

}
