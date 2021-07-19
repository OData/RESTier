using System.Security.Claims;

namespace Microsoft.Restier.AspNetCore.Abstractions
{

    /// <summary>
    /// 
    /// </summary>
    public interface IClaimsPrincipalAccessor
    {

        /// <summary>
        /// 
        /// </summary>
        public ClaimsPrincipal ClaimsPrincipal { get; set; }

    }

}
