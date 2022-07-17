#if NET6_0_OR_GREATER

using Microsoft.Restier.EntityFrameworkCore;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using System;

namespace Microsoft.Restier.Tests.EntityFrameworkCore.Scenarios.Views
{

    /// <summary>
    /// 
    /// </summary>
    public class LibraryWithViewsApi : EntityFrameworkApi<LibraryWithViewsContext>
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serviceProvider"></param>
        public LibraryWithViewsApi(IServiceProvider serviceProvider) : base(serviceProvider)
        {

        }

    }

}

#endif