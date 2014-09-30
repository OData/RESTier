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
    using Submit;

    /// <summary>
    /// Represents context under which a domain operates.
    /// </summary>
    /// <remarks>
    /// A domain context is an instantiation of a domain configuration. It
    /// maintains a set of properties that can be used to share instance
    /// data between hook points.
    /// </remarks>
    public class DomainContext : PropertyBag
    {
        /// <summary>
        /// Initializes a new domain context.
        /// </summary>
        /// <param name="configuration">
        /// A domain configuration.
        /// </param>
        public DomainContext(DomainConfiguration configuration)
        {
            Ensure.NotNull(configuration, "configuration");
            if (!configuration.IsCommitted)
            {
                // TODO: error message
                throw new ArgumentException();
            }
            this.Configuration = configuration;
        }

        /// <summary>
        /// Gets the domain configuration.
        /// </summary>
        public DomainConfiguration Configuration { get; private set; }

        /// <summary>
        /// Gets a value indicating if this domain
        /// context is current submitting changes.
        /// </summary>
        public bool IsSubmitting { get; internal set; }
    }
}
