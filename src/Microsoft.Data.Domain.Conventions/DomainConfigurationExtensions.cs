// Domain Framework ver. 1.0
// Copyright (c) Microsoft Corporation
// All rights reserved.
// MIT License
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
// associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute,
// sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial
// portions of the Software.
// 
// THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
// NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES
// OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;

namespace Microsoft.Data.Domain
{
    using System.ComponentModel;
    using Conventions;
    using Submit;

    /// <summary>
    /// Provides a set of static (Shared in Visual Basic)
    /// methods for interacting with objects that implement
    /// <see cref="DomainConfiguration"/>.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class DomainConfigurationExtensions
    {
        /// <summary>
        /// Enables code-based conventions for a domain.
        /// </summary>
        /// <param name="configuration">
        /// A domain configuration.
        /// </param>
        /// <param name="targetType">
        /// The type of a class on which code-based conventions are used.
        /// </param>
        /// <remarks>
        /// This method adds hook points to the domain configuration that
        /// inspect a target type for a variety of code-based conventions
        /// such as usage of specific attributes or members that follow
        /// certain naming conventions.
        /// </remarks>
        public static void EnableConventions(
            this DomainConfiguration configuration,
            Type targetType)
        {
            Ensure.NotNull(configuration, "configuration");
            Ensure.NotNull(targetType, "targetType");

            ConventionalChangeSetAuthorizer.ApplyTo(configuration, targetType);
            ConventionalChangeSetEntryFilter.ApplyTo(configuration, targetType);
            configuration.AddHookPoint(typeof(IChangeSetEntryValidator),
                ConventionalChangeSetEntryValidator.Instance);
            ConventionalEntitySetProvider.ApplyTo(configuration, targetType);
            ConventionalEntitySetFilter.ApplyTo(configuration, targetType);
        }
    }
}
