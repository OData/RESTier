// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests;

/// <summary>
/// Minimal authentication handler for integration tests. Reads the <c>X-Test-User</c> request
/// header: when present, constructs a <see cref="ClaimsPrincipal"/> with <c>Name == "TestUser"</c>
/// and a <c>Role</c> claim taken from the header value. Anonymous requests (no header) produce a
/// "no result" — the standard
/// <see cref="Microsoft.AspNetCore.Authorization.AuthorizationMiddleware"/> then enforces or skips
/// authorization per endpoint metadata.
/// </summary>
internal sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Test";
    public const string HeaderName = "X-Test-User";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(HeaderName, out var headerValues) || headerValues.Count == 0)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var role = headerValues[0];
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "TestUser"),
        };
        if (!string.IsNullOrEmpty(role))
        {
            claims.Add(new Claim(ClaimTypes.Role, role.ToLowerInvariant()));
        }

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
