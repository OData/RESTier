// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
#if !EF7
using System.Data.Entity;
#endif
#if EF7
using Microsoft.EntityFrameworkCore;
#endif
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Core;

namespace Microsoft.Restier.EntityFramework
{
    /// <summary>
    /// Represents an API over a DbContext.
    /// </summary>
    /// <typeparam name="T">The DbContext type.</typeparam>
    /// <remarks>
    /// <para>
    /// This class tries to instantiate <typeparamref name="T"/> with the best matched constructor
    /// base on services configured. Descendants could override by registering <typeparamref name="T"/>
    /// as a scoped service. But in this case, proxy creation must be disabled in the constructors of
    /// <typeparamref name="T"/> under Entity Framework 6.
    /// </para>
    /// </remarks>
#if EF7
    [CLSCompliant(false)]
#endif
    public class EntityFrameworkApi<T> : ApiBase where T : DbContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EntityFrameworkApi{T}" /> class.
        /// </summary>
        /// <param name="serviceProvider">
        /// An <see cref="IServiceProvider"/> containing all services of this <see cref="EntityFrameworkApi{T}"/>.
        /// </param>
        public EntityFrameworkApi(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        /// <summary>
        /// Gets the underlying DbContext for this API.
        /// </summary>
        public T DbContext
        {
            get
            {
                return (T)this.GetApiService<DbContext>();
            }
        }

    }

}