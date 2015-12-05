using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.Restier.Core
{
    [CLSCompliant(false)]
    public interface IApiScopeFactory
    {
        IServiceScope CreateApiScope();
    }
}
