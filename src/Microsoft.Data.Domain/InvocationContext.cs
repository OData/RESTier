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
using System.Collections.Generic;

namespace Microsoft.Data.Domain
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
        /// Initializes a new invocation context.
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
        /// Gets the single instance of a type of singleton hook point.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the singleton hook point.
        /// </typeparam>
        /// <returns>
        /// The single instance of the specified type of singleton hook
        /// point, or <c>null</c> if the domain configuration does not
        /// have an instance of the specified type of singleton hook point.
        /// </returns>
        public T GetHookPoint<T>()
            where T : class
        {
            return this.DomainContext.Configuration.GetHookPoint<T>();
        }

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
    }
}
