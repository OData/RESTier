// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Threading;

namespace Microsoft.Restier.Tests.Shared
{
    /// <summary>
    /// Cross-process named semaphore used by test assemblies that share access to
    /// the LocalDB-backed LibraryContext / MarvelContext databases. Assemblies that
    /// hit those databases acquire the lock in <c>[AssemblyInitialize]</c> and
    /// release it in <c>[AssemblyCleanup]</c>, serialising their test-host
    /// processes against each other regardless of TFM or project. Assemblies that
    /// do not touch shared LocalDB resources do not reference this lock and run in
    /// full parallel.
    /// </summary>
    /// <remarks>
    /// Named OS semaphores in .NET are Windows-only (Unix throws
    /// <c>PlatformNotSupportedException</c> for the named-constructor overload).
    /// LocalDB itself is also Windows-only, so on non-Windows hosts the lock is a
    /// no-op — the tests that would need the lock are either skipped or hit
    /// in-memory stores instead.
    /// </remarks>
    public static class SharedLocalDbLock
    {
        // The "Global\" prefix scopes the semaphore to all sessions on the
        // machine, so two `dotnet test` processes — even started from different
        // user sessions — synchronise correctly. For per-user scoping use
        // "Local\". "Global\" is the right default for CI agents and
        // dev-box solution runs.
        private const string Name = @"Global\RESTier_SharedLocalDb_AssemblyLock";

        private static Semaphore _semaphore;

        /// <summary>
        /// Acquires the cross-process lock. Call from <c>[AssemblyInitialize]</c>.
        /// On non-Windows hosts this method is a no-op.
        /// </summary>
        public static void Acquire()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            if (_semaphore is not null)
            {
                // Already acquired in this process — no-op to avoid leaking the
                // existing handle and to prevent a self-deadlock from a re-entrant
                // WaitOne() on a count=1 semaphore.
                return;
            }

            // initialCount=1, maximumCount=1 → mutual exclusion. The out parameter
            // (createdNew) is intentionally discarded: we don't care which process
            // created the OS handle first.
            _semaphore = new Semaphore(initialCount: 1, maximumCount: 1, name: Name, out _);
            _semaphore.WaitOne();
        }

        /// <summary>
        /// Releases the cross-process lock. Call from <c>[AssemblyCleanup]</c>.
        /// Safe to call when <see cref="Acquire"/> was a no-op.
        /// </summary>
        public static void Release()
        {
            if (_semaphore is null)
            {
                return;
            }

            _semaphore.Release();
            _semaphore.Dispose();
            _semaphore = null;
        }
    }
}
