// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.Restier.Tests.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.AspNetCore.Versioning
{
    [TestClass]
    public static class AssemblyHooks
    {
        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext _) => SharedLocalDbLock.Acquire();

        [AssemblyCleanup]
        public static void AssemblyCleanup() => SharedLocalDbLock.Release();
    }
}
