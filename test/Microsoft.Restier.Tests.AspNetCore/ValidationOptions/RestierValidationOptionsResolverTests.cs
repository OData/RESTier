// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Diagnostics;
using FluentAssertions;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Query.Validator;
using Microsoft.Restier.AspNetCore.Routing;
using Microsoft.Restier.Core;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.ValidationOptions;

public class RestierValidationOptionsResolverTests
{
    private sealed class CapturingTraceListener : TraceListener
    {
        public System.Collections.Generic.List<string> Warnings { get; } = new();

        public override void Write(string message) { }

        public override void WriteLine(string message) => Warnings.Add(message);

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
        {
            if (eventType == TraceEventType.Warning)
            {
                Warnings.Add(message);
            }
        }
    }

    private static (CapturingTraceListener listener, System.IDisposable scope) AttachListener()
    {
        var listener = new CapturingTraceListener();
        Trace.Listeners.Add(listener);
        return (listener, new TraceListenerScope(listener));
    }

    private sealed class TraceListenerScope : System.IDisposable
    {
        private readonly TraceListener listener;
        public TraceListenerScope(TraceListener listener) { this.listener = listener; }
        public void Dispose() => Trace.Listeners.Remove(listener);
    }

    [Fact]
    public void Resolve_EmptyBag_NoGlobalMaxTop_ProducesFrameworkDefaults()
    {
        var bag = new RestierValidationOptions();
        var globalOptions = new ODataOptions();

        var resolved = RestierValidationOptionsResolver.Resolve(bag, globalOptions, routePrefix: "api");

        resolved.Should().NotBeNull();
        resolved.MaxTop.Should().BeNull();
        resolved.MaxExpansionDepth.Should().Be(new ODataValidationSettings().MaxExpansionDepth);
    }

    [Fact]
    public void Resolve_EmptyBag_GlobalMaxTopSet_InheritsGlobalMaxTop()
    {
        var bag = new RestierValidationOptions();
        var globalOptions = new ODataOptions();
        globalOptions.SetMaxTop(50);

        var resolved = RestierValidationOptionsResolver.Resolve(bag, globalOptions, routePrefix: "api");

        resolved.MaxTop.Should().Be(50);
    }

    [Fact]
    public void Resolve_BagMaxTop_GlobalMaxTopDisagrees_BagWinsAndEmitsWarning()
    {
        var (listener, scope) = AttachListener();
        using var _ = scope;

        var bag = new RestierValidationOptions { MaxTop = 25 };
        var globalOptions = new ODataOptions();
        globalOptions.SetMaxTop(50);

        var resolved = RestierValidationOptionsResolver.Resolve(bag, globalOptions, routePrefix: "api");

        resolved.MaxTop.Should().Be(25);
        listener.Warnings.Should().ContainSingle(w =>
            w.Contains("MaxTop", System.StringComparison.Ordinal) &&
            w.Contains("api", System.StringComparison.Ordinal) &&
            w.Contains("25", System.StringComparison.Ordinal) &&
            w.Contains("50", System.StringComparison.Ordinal));
    }

    [Fact]
    public void Resolve_BagMaxTop_GlobalMaxTopAgrees_NoWarning()
    {
        var (listener, scope) = AttachListener();
        using var _ = scope;

        var bag = new RestierValidationOptions { MaxTop = 50 };
        var globalOptions = new ODataOptions();
        globalOptions.SetMaxTop(50);

        var resolved = RestierValidationOptionsResolver.Resolve(bag, globalOptions, routePrefix: "api");

        resolved.MaxTop.Should().Be(50);
        listener.Warnings.Should().BeEmpty();
    }

    [Fact]
    public void Resolve_BagSetsAllFields_AllFlowThrough()
    {
        var bag = new RestierValidationOptions
        {
            MaxTop = 10,
            MaxSkip = 1000,
            MaxExpansionDepth = 3,
            MaxAnyAllExpressionDepth = 2,
            MaxOrderByNodeCount = 4,
            MaxNodeCount = 50,
        };

        var resolved = RestierValidationOptionsResolver.Resolve(bag, new ODataOptions(), routePrefix: "api");

        resolved.MaxTop.Should().Be(10);
        resolved.MaxSkip.Should().Be(1000);
        resolved.MaxExpansionDepth.Should().Be(3);
        resolved.MaxAnyAllExpressionDepth.Should().Be(2);
        resolved.MaxOrderByNodeCount.Should().Be(4);
        resolved.MaxNodeCount.Should().Be(50);
    }

    [Fact]
    public void Build_BagMaxTop_GlobalDisagrees_BagWinsAndDoesNotEmitWarning()
    {
        var (listener, scope) = AttachListener();
        using var _ = scope;

        var bag = new RestierValidationOptions { MaxTop = 25 };
        var globalOptions = new ODataOptions();
        globalOptions.SetMaxTop(50);

        var built = RestierValidationOptionsResolver.Build(bag, globalOptions);

        built.MaxTop.Should().Be(25);
        listener.Warnings.Should().BeEmpty(
            because: "Build is the silent per-request path; the conflict warning is only emitted by Resolve at route-add time");
    }

    [Fact]
    public void Build_NullODataOptions_DoesNotThrow()
    {
        var bag = new RestierValidationOptions { MaxExpansionDepth = 4 };

        var built = RestierValidationOptionsResolver.Build(bag, globalOptions: null);

        built.MaxExpansionDepth.Should().Be(4);
        built.MaxTop.Should().BeNull();
    }
}
