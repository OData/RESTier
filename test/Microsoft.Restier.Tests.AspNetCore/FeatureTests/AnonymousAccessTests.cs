// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using CloudNimble.Breakdance.AspNetCore;
using CloudNimble.EasyAF.Http.OData;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.OData;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.AspNetCore;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Core;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests;

/// <summary>
/// Integration tests for #717: ASP.NET Core's standard [AllowAnonymous] / [Authorize] /
/// [Authorize(Policy=...)] honored on the RESTier API class and on operation methods.
///
/// Each test builds a fresh <see cref="RestierBreakdanceTestBase{TApi}"/> with:
/// - The "Test" auth scheme registered via DI (X-Test-User header drives the principal).
/// - The "AdminOnly" policy registered when the fixture uses it.
/// - A global <see cref="AuthorizeFilter"/> applied so every endpoint requires auth unless
///   explicitly overridden by [AllowAnonymous].
/// - UseAuthentication() injected via ApplicationBuilderAction (Breakdance's pipeline runs this
///   hook *before* UseRouting, so the principal is populated before the matcher policy and
///   authorization middleware see the endpoint).
/// </summary>
public class AnonymousAccessTests
{
    private static RestierBreakdanceTestBase<TApi> BuildHost<TApi>(bool addAdminPolicy = false)
        where TApi : ApiBase
    {
        var testBase = new RestierBreakdanceTestBase<TApi>();

        testBase.TestHostBuilder.ConfigureServices((_, services) =>
        {
            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });

            services.AddAuthorization(o =>
            {
                if (addAdminPolicy)
                {
                    o.AddPolicy("AdminOnly", p => p.RequireRole("admin"));
                }
            });

            // Global [Authorize] filter — applies to every endpoint unless overridden.
            services.Configure<MvcOptions>(o => o.Filters.Add(new AuthorizeFilter()));
        });

        testBase.AddRestierAction = (odataOptions) =>
        {
            odataOptions.AddRestierRoute<TApi>(WebApiConstants.RouteName, restierServices =>
            {
            },
            options =>
            {
                options.Validation.MaxTop = 5;
                options.Validation.MaxAnyAllExpressionDepth = 3;
                options.Validation.MaxExpansionDepth = 3;
            });
        };

        testBase.ApplicationBuilderAction = builder => builder.UseAuthentication();

        testBase.TestSetup();
        return testBase;
    }

    private static async Task<HttpResponseMessage> SendAsync<TApi>(
        RestierBreakdanceTestBase<TApi> host,
        HttpMethod method,
        string resource,
        string asUser = null)
        where TApi : ApiBase
    {
        // WebApiConstants.Localhost = "http://localhost/" and RoutePrefix = "api/tests/" (trailing slash).
        // resource starts with "/" or is "/" itself; trim its leading slash so we don't double up.
        var relative = resource.StartsWith('/') ? resource.Substring(1) : resource;
        var url = $"{WebApiConstants.Localhost}{WebApiConstants.RoutePrefix}{relative}";

        var client = host.GetHttpClient(WebApiConstants.RoutePrefix);
        using var message = new HttpRequestMessage(method, url);
        message.Headers.Add("Accept", ODataConstants.DefaultAcceptHeader);
        if (asUser is not null)
        {
            message.Headers.Add(TestAuthHandler.HeaderName, asUser);
        }
        return await client.SendAsync(message);
    }

    #region Class-level

    // Class-level scenarios exercise $metadata: it's always served by RestierController, doesn't
    // require entity-set query plumbing, and the metadata path resolves to "class" target key —
    // exactly the surface we want to test for class-level [AllowAnonymous] / [Authorize].

    [Fact]
    public async Task ClassAllowAnonymous_MetadataAccessibleAnonymously()
    {
        // Global [Authorize] + class [AllowAnonymous] + anonymous GET /$metadata → 200.
        using var host = BuildHost<AnonymousAtClassApi>();
        var response = await SendAsync(host, HttpMethod.Get, "/$metadata");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task NoClassAttribute_AnonymousRequest_Returns401()
    {
        // Control case: global [Authorize], no class attribute, anonymous GET /$metadata → 401.
        using var host = BuildHost<RequireAuthApi>();
        var response = await SendAsync(host, HttpMethod.Get, "/$metadata");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ClassAllowAnonymous_ServiceDocumentAccessible()
    {
        // Service document (GET /) + class [AllowAnonymous] → 200.
        using var host = BuildHost<AnonymousAtClassApi>();
        var response = await SendAsync(host, HttpMethod.Get, "/");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Operation method

    [Fact]
    public async Task OperationAllowAnonymous_AccessibleAnonymously()
    {
        // Scenario 5: [AllowAnonymous] on action → anonymous POST /Hello must NOT be denied
        // by AuthorizationMiddleware. We assert "not 401/403" rather than the success status
        // because RESTier's action-execution path can return 500 in test setups that don't
        // wire up an OData batch fixture, and that 500 is not what this test is about.
        using var host = BuildHost<AnonymousAtOperationApi>(addAdminPolicy: true);
        var response = await SendAsync(host, HttpMethod.Post, "/Hello");

        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task OperationWithAdminPolicy_AdminUser_Allowed()
    {
        // Scenario 7: [Authorize(Policy = "AdminOnly")] on action, authenticated admin: auth passes.
        using var host = BuildHost<AnonymousAtOperationApi>(addAdminPolicy: true);
        var response = await SendAsync(host, HttpMethod.Post, "/AdminGreeting", asUser: "Admin");

        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task OperationWithAdminPolicy_NonAdminUser_Returns403()
    {
        // Scenario 6: same operation, authenticated non-admin user → 403.
        using var host = BuildHost<AnonymousAtOperationApi>(addAdminPolicy: true);
        var response = await SendAsync(host, HttpMethod.Post, "/AdminGreeting", asUser: "User");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task OperationWithoutAttribute_AnonymousReturns401()
    {
        // Operation method with no attribute inherits the global [Authorize] filter.
        using var host = BuildHost<AnonymousAtOperationApi>(addAdminPolicy: true);
        var response = await SendAsync(host, HttpMethod.Post, "/DefaultGreeting");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Inheritance

    [Fact]
    public async Task InheritedAuthorize_AnonymousReturns401()
    {
        // Subclass with no override inherits [Authorize] from the base class.
        using var host = BuildHost<InheritsAuthApi>();
        var response = await SendAsync(host, HttpMethod.Get, "/$metadata");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task InheritedAuthorize_AuthenticatedUserSucceeds()
    {
        // Same inheritance, authenticated user → 200.
        using var host = BuildHost<InheritsAuthApi>();
        var response = await SendAsync(host, HttpMethod.Get, "/$metadata", asUser: "User");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion
}
