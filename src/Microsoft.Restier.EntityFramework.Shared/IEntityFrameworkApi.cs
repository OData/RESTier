using System;
using System.Collections.Generic;
#if EFCore
using Microsoft.EntityFrameworkCore;
#else
using System.Data.Entity;
#endif
using System.Text;

#if EFCore
namespace Microsoft.Restier.EntityFrameworkCore
#else
namespace Microsoft.Restier.EntityFramework
#endif
{
    /// <summary>
    /// Interface for Entity Framework Api instances.
    /// Makes easy retrieval of the DbContext possible.
    /// </summary>
    public interface IEntityFrameworkApi
    {
        /// <summary>
        /// Gets the underlying DbContext for this API.
        /// </summary>
        public DbContext DbContext { get; }

        /// <summary>
        /// Gets the Context Type.
        /// </summary>
        public Type ContextType { get; }
    }
}
