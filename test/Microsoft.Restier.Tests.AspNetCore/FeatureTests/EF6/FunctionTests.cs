// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.EntityFramework;
using Microsoft.Restier.Tests.Shared.Scenarios.Library.EF6;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests.EF6;

[Collection("LibraryApiEF6")]
public class FunctionTests(ITestOutputHelper outputHelper) : FunctionTests<LibraryApi, LibraryContext>(outputHelper)
{
    protected override Action<IServiceCollection> ConfigureServices
        => services =>
        {
            // BoundFunctions tests exercise OnExecuting*/OnExecuted* interceptors
            // that mutate entities via repeated IQueryable<T>.ToList() materialization,
            // a pattern that only works under tracked semantics (it relies on
            // change-tracker identity-mapping across multiple materializations of
            // the same IQueryable). The post-#726 no-tracking default materializes
            // fresh instances each .ToList(), so the mutations are lost. Opt these
            // tests into TrackAll to preserve their interceptor scenario, which
            // also demonstrates the documented escape hatch.
            //
            // RestierEFOptions is registered with TryAddSingleton inside
            // AddEFProviderServices, so our override must be added first.
            services.AddSingleton(new RestierEFOptions { TrackingBehavior = RestierEFTrackingBehavior.TrackAll });
            services.AddEntityFrameworkServices<LibraryContext>();
        };
}
