// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Tests.Shared.Scenarios.Library.EF6;
using Microsoft.Restier.Tests.Shared.Scenarios.Marvel.EF6;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests.EF6;

[TestClass]
[DoNotParallelize]
public class MetadataTests : MetadataTests<LibraryApi, LibraryContext>
{
    protected override Action<IServiceCollection> ConfigureServices
        => services => services.AddEntityFrameworkServices<LibraryContext>();

    protected override string ProviderName => "EF6";

    protected override string MarvelBaselinePrefix => "MarvelApi-EF6";

    protected override async Task<XDocument> GetMarvelApiMetadataAsync()
    {
        return await RestierTestHelpers.GetApiMetadataAsync<MarvelApi>(
            serviceCollection: services => services.AddEntityFrameworkServices<MarvelContext>());
    }

}
