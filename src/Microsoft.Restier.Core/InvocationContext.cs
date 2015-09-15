// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Restier.Core
{
    /// <summary>
    /// Represents context under which a domain flow operates.
    /// </summary>
    /// <remarks>
    /// An invocation context is created each time a domain is invoked and
    /// is used for a specific domain flow. It maintains a set of properties
    /// that can store data that lives for the lifetime of the flow.
    /// </remarks>
    public class InvocationContext : PropertyBag
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InvocationContext" /> class.
        /// </summary>
        /// <param name="domainContext">
        /// A domain context.
        /// </param>
        public InvocationContext(DomainContext domainContext)
        {
            Ensure.NotNull(domainContext, "domainContext");
            this.DomainContext = domainContext;
        }

        /// <summary>
        /// Gets the domain context.
        /// </summary>
        public DomainContext DomainContext { get; private set; }

        /// <summary>
        /// Gets all instances of a type of multi-cast hook point.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the multi-cast hook point.
        /// </typeparam>
        /// <returns>
        /// All instances of the specified type of multi-cast
        /// hook point in the original order of registration.
        /// </returns>
        public IEnumerable<T> GetHookPoints<T>()
            where T : class
        {
            return this.DomainContext.Configuration.GetHookPoints<T>();
        }

        internal T GetHookHandler<T>() where T : class, IHookHandler
        {
            return this.DomainContext.Configuration.GetHookHandler<T>();
        }
    }
}
