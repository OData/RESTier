using System;
using System.Collections.Generic;
using System.Text;

#if NETCOREAPP3_1_OR_GREATER
namespace Microsoft.Restier.AspNetCore.Model
#else
namespace Microsoft.Restier.AspNet.Model
#endif
{

    /// <summary>
    /// 
    /// </summary>
    public class UnboundOperationAttribute : OperationAttribute
    {

        /// <summary>
        /// Gets or sets the entity set associated with the operation result.
        /// </summary>
        public string EntitySet { get; set; }

    }

}
