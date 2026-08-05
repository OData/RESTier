// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Tests.Shared.Scenarios.Library.EFCore;
using Microsoft.Restier.Tests.Shared.Scenarios.Marvel.EFCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.AspNetCore.RegressionTests.EFCore;

[TestClass]
[DoNotParallelize]
public class Issue671_MultipleContexts_SingleLibraryContext
    : Issue671_MultipleContexts_SingleLibraryContext<LibraryApi, LibraryContext>
{
    protected override Action<IServiceCollection> ConfigureServices
        => services => services.AddEntityFrameworkServices<LibraryContext>();
}

[TestClass]
[DoNotParallelize]
public class Issue671_MultipleContexts_SingleMarvelContext
    : Issue671_MultipleContexts_SingleMarvelContext<MarvelApi, MarvelContext>
{
    protected override Action<IServiceCollection> ConfigureServices
        => services => services.AddEntityFrameworkServices<MarvelContext>();
}

[TestClass]
[DoNotParallelize]
public class Issue671_MultipleContexts : Issue671_MultipleContexts<LibraryApi, MarvelApi>
{
    protected override Action<IServiceCollection> ConfigureLibraryServices
        => services => services.AddEntityFrameworkServices<LibraryContext>();

    protected override Action<IServiceCollection> ConfigureMarvelServices
        => services => services.AddEntityFrameworkServices<MarvelContext>();
}
