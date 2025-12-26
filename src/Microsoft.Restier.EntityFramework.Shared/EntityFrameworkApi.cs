// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
#if EFCore
using Microsoft.EntityFrameworkCore;
#else
using System.Data.Entity;
#endif
using Microsoft.Restier.Core;

#if EFCore
namespace Microsoft.Restier.EntityFrameworkCore
#else
namespace Microsoft.Restier.EntityFramework
#endif
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
    public class EntityFrameworkApi<T> : ApiBase, IEntityFrameworkApi
        where T : DbContext
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
                return this.GetApiService<T>();
            }
        }

        /// <summary>
        /// Gets the Context Type.
        /// </summary>
        public Type ContextType => typeof(T);

        /// <summary>
        /// Gets the underlying DbContext for this API.
        /// </summary>
        DbContext IEntityFrameworkApi.DbContext => DbContext;
    }
}