using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Restier.EntityFramework
{
    /// <summary>
    /// Interface to implement by Api classes to get the context.
    /// </summary>
    public interface IDbContextProvider
    {
        /// <summary>
        /// Gets the underlying DbContext for this API.
        /// </summary>
        DbContext DbContext { get; }
    }
}
