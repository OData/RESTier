// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.Extensions.Options;
using System;
using System.Linq;

namespace Microsoft.Restier.AspNetCore.Options;

/// <summary>
/// Restier options to change the Base URI on the formatters if necessary.
/// </summary>
internal class RestierMvcOptionsSetup : IConfigureOptions<MvcOptions>
{
    private readonly Uri _alternateBaseUri;

    /// <summary>
    /// Restier options to change the Base URI on the formatters if necessary.
    /// </summary>
    /// <param name="alternateBaseUri">The alternate Base URI to use.</param>
    public RestierMvcOptionsSetup(Uri alternateBaseUri)
    {
        Ensure.NotNull(alternateBaseUri, nameof(alternateBaseUri));
        _alternateBaseUri = alternateBaseUri;
    }

    /// <summary>
    /// Configures the specified <see cref="MvcOptions"/> to use the provided alternate base URI for OData formatters.
    /// This should be run after the ODataMvcOptionsSetup has been executed, as it relies on the OData formatters being present in the options.
    /// </summary>
    /// <param name="options">The <see cref="MvcOptions"/> instance to configure.</param>
    public void Configure(MvcOptions options)
    {
        Ensure.NotNull(options, nameof(options));

        // Read formatters
        Uri InputBaseAddressFactory(HttpRequest request) =>
            new(_alternateBaseUri, ODataInputFormatter.GetDefaultBaseAddress(request).AbsolutePath);

        foreach (var formatter in options.InputFormatters.OfType<ODataInputFormatter>())
        {
            formatter.BaseAddressFactory = InputBaseAddressFactory;
        }

        // Write formatters
        Uri OutputBaseAddressFactory(HttpRequest request) =>
            new(_alternateBaseUri, ODataOutputFormatter.GetDefaultBaseAddress(request).AbsolutePath);

        foreach (var formatter in options.OutputFormatters.OfType<ODataOutputFormatter>())
        {
            formatter.BaseAddressFactory = OutputBaseAddressFactory;
        }
    }
}