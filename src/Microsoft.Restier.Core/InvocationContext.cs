// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;

namespace Microsoft.Restier.Core
{
    /// <summary>
    /// Represents context under which an request is processed.
    /// The request could be a query, a submit, an operation execution or a model retrieve.
    /// It has subclass for each kinds of request.
    /// </summary>
    /// <remarks>
    /// An invocation context is created each time an request is parsed to a specified request.
    /// </remarks>
    public class InvocationContext
    {
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
        }

        /// <summary>
        /// Gets the API context.
        /// </summary>
        public ApiContext ApiContext { get; private set; }
    }
}
