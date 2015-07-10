// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;

namespace Microsoft.Restier.Core
{
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
        /// Initializes a new instance of the <see cref="DomainContext" /> class.
        /// </summary>
        /// <param name="configuration">
        /// A domain configuration.
        /// </param>
        public DomainContext(DomainConfiguration configuration)
        {
            Ensure.NotNull(configuration, "configuration");
            if (!configuration.IsCommitted)
            {
                // TODO GitHubIssue#24 : error message
                throw new ArgumentException();
            }
            this.Configuration = configuration;
        }

        /// <summary>
        /// Gets the domain configuration.
        /// </summary>
        public DomainConfiguration Configuration { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this domain
        /// context is current submitting changes.
        /// </summary>
        public bool IsSubmitting { get; internal set; }
    }
}
