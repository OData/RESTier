// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Core;
using Microsoft.Restier.Tests.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests;

public abstract class MetadataTests<TApi, TContext> : RestierTestBase<TApi>
    where TApi : ApiBase
    where TContext : class
{
    private const string RelativePath = "..//..//..//..//Microsoft.Restier.Tests.AspNetCore//";
    private const string BaselineFolder = "Baselines//";

    protected abstract Action<IServiceCollection> ConfigureServices { get; }

    protected abstract Task<XDocument> GetMarvelApiMetadataAsync();

    /// <summary>
    /// Gets the provider-specific prefix for baseline filenames (e.g., "EF6" or "EFCore").
    /// </summary>
    protected abstract string ProviderName { get; }

    /// <summary>
    /// Gets the provider-specific prefix for Marvel baseline filenames.
    /// </summary>
    protected abstract string MarvelBaselinePrefix { get; }

    [TestMethod]
    public async Task LibraryApi_CompareCurrentApiMetadataToPriorRun()
    {
        var fileName = $"{Path.Combine(RelativePath, BaselineFolder)}{typeof(TApi).Name}-{ProviderName}-ApiMetadata.txt";
        File.Exists(fileName).Should().BeTrue();

        var oldReport = File.ReadAllText(fileName);
        var newReport = await RestierTestHelpers.GetApiMetadataAsync<TApi>(
            serviceCollection: ConfigureServices);

        TraceListener.WriteLine($"Old Report: {oldReport}");
        TraceListener.WriteLine($"New Report: {newReport}");

        oldReport.Should().BeEquivalentTo(newReport.ToString());
    }

    [TestMethod]
    public async Task MarvelApi_CompareCurrentApiMetadataToPriorRun()
    {
        var fileName = $"{Path.Combine(RelativePath, BaselineFolder)}{MarvelBaselinePrefix}-ApiMetadata.txt";
        File.Exists(fileName).Should().BeTrue();

        var oldReport = File.ReadAllText(fileName);
        var newReport = await GetMarvelApiMetadataAsync();

        TraceListener.WriteLine($"Old Report: {oldReport}");
        TraceListener.WriteLine($"New Report: {newReport}");

        oldReport.Should().BeEquivalentTo(newReport.ToString());
    }

    [TestMethod]
    public async Task StoreApi_CompareCurrentApiMetadataToPriorRun()
    {
        var fileName = $"{Path.Combine(RelativePath, BaselineFolder)}{nameof(StoreApi)}-ApiMetadata.txt";
        File.Exists(fileName).Should().BeTrue();

        var oldReport = File.ReadAllText(fileName);
        var newReport = await RestierTestHelpers.GetApiMetadataAsync<StoreApi>();

        TraceListener.WriteLine($"Old Report: {oldReport}");
        TraceListener.WriteLine($"New Report: {newReport}");

        oldReport.Should().BeEquivalentTo(newReport.ToString());
    }
}
