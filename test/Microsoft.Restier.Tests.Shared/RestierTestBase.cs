// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;

namespace Microsoft.Restier.Tests.Shared
{
    /// <summary>
    /// Common base for Restier test classes. Adds a trace listener that captures output
    /// for inspection by individual tests.
    /// </summary>
    public class RestierTestBase<TApi> : RestierBreakdanceTestBase<TApi>
        where TApi : ApiBase
    {
        public RestierTestBase()
        {
            Trace.Listeners.Add(TraceListener);
        }

        /// <summary>
        /// Gets or sets the MSTest test context. Populated by the runner.
        /// </summary>
        public TestContext TestContext { get; set; }

        /// <summary>
        /// Gets the trace listener that can be used for test output.
        /// </summary>
        public TraceListener TraceListener { get; } = new TestTraceListener();
    }
}
