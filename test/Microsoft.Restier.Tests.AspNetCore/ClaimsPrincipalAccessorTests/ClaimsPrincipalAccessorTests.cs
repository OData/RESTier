// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

#if NET6_0_OR_GREATER

using CloudNimble.Breakdance.AspNetCore;
using CloudNimble.EasyAF.Http.OData;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.AspNetCore;
using Microsoft.Restier.Core.Submit;
using Microsoft.Restier.Tests.AspNetCore.ClaimsPrincipalAccessor;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.Restier.Tests.AspNetCore;

[TestClass]
public class ClaimsPrincipalAccessorTests : RestierTestBase<ClaimsPrincipalApi>
{
    public ClaimsPrincipalAccessorTests()
    {
        ApplicationBuilderAction = app =>
        {
            app.UseClaimsPrincipals();
        };
        AddRestierAction = options =>
        {
            options.AddRestierRoute<ClaimsPrincipalApi>(WebApiConstants.RoutePrefix, services =>
            {
                services.AddSingleton<IChangeSetInitializer, DefaultChangeSetInitializer>();
                services.AddSingleton<ISubmitExecutor, DefaultSubmitExecutor>();
            });
        };
        TestSetup();
    }

    [TestMethod]
    public async Task ClaimsPrincipalCurrent_IsNotNull()
    {
        var response = await ExecuteTestRequest(HttpMethod.Get, resource: "/ClaimsPrincipalCurrentIsNotNull()");
        _ = await TraceListener.LogAndReturnMessageContentAsync(response);

        response.IsSuccessStatusCode.Should().BeTrue();
        var (Response, ErrorContent) = await response.DeserializeResponseAsync<ODataV4PrimitiveResult<bool>>();
        Response.Should().NotBeNull();
        Response.Value.Should().BeTrue();
    }
}

#endif
