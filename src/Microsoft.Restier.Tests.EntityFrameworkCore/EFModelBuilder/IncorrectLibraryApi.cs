using Microsoft.Restier.EntityFrameworkCore;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using System;

namespace Microsoft.Restier.Tests.EntityFrameworkCore.EFModelBuilderScenario
{
    /// <summary>
    /// 
    /// </summary>
    public class IncorrectLibraryApi : EntityFrameworkApi<IncorrectLibraryContext>
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serviceProvider"></param>
        public IncorrectLibraryApi(IServiceProvider serviceProvider) : base(serviceProvider)
        {

        }

    }

}
