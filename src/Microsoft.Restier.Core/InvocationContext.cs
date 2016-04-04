// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Restier.Core
{
    /// <summary>
    /// Represents context under which an API flow operates.
    /// </summary>
    /// <remarks>
    /// An invocation context is created each time an API is invoked and
    /// is used for a specific API flow.
    /// </remarks>
    public class InvocationContext
    {
        private readonly IServiceScope scope;

        /// <summary>
        /// Initializes a new instance of the <see cref="InvocationContext" /> class.
        /// </summary>
        /// <param name="apiContext">
        /// An API context.
        /// </param>
        public InvocationContext(ApiContext apiContext)
        {
            Ensure.NotNull(apiContext, "apiContext");

            this.ApiContext = apiContext;
            this.scope = apiContext.ServiceProvider
                .GetRequiredService<IServiceScopeFactory>().CreateScope();
        }

        /// <summary>
        /// Gets the API context.
        /// </summary>
        public ApiContext ApiContext { get; private set; }

        /// <summary>
        /// Gets the <see cref="IServiceProvider"/> which contains all services of this <see cref="ApiContext"/>.
        /// </summary>
        public IServiceProvider ServiceProvider
        {
            get { return this.scope.ServiceProvider; }
        }

        internal void DisposeScope()
        {
            this.scope.Dispose();
        }
    }
}
