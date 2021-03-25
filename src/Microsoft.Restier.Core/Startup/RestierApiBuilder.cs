using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Restier.Core.Startup
{

    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// The implementation of adding specific APIs is left to the implementing Web framework, either in ASP.NET or ASP.NET Core.
    /// The reason being that adding APIs requires Web runtime-speicific services that the Restier Core library is not aware of.
    /// </remarks>
    public class RestierApiBuilder
    {

        #region Internal Properties

        /// <summary>
        /// 
        /// </summary>
        internal Dictionary<Type, Action<IServiceCollection>> Apis { get; private set; }

        #endregion

        #region Constructors

        /// <summary>
        /// 
        /// </summary>
        public RestierApiBuilder()
        {
            Apis = new();
        }

        #endregion

    }

}