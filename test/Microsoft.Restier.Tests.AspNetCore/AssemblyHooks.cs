// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.Restier.Tests.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.AspNetCore
{
    /// <summary>
    /// Per-assembly setup that acquires <see cref="SharedLocalDbLock"/> for the
    /// duration of this assembly's test-host process. See SharedLocalDbLock for
    /// the rationale (cross-process serialisation against other LocalDB-touching
    /// test assemblies).
    /// </summary>
    [TestClass]
    public static class AssemblyHooks
    {
        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext _) => SharedLocalDbLock.Acquire();

        [AssemblyCleanup]
        public static void AssemblyCleanup() => SharedLocalDbLock.Release();
    }
}
