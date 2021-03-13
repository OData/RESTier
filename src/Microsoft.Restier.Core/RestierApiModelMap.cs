using System;
using Microsoft.OData.Edm;

namespace Microsoft.Restier.Core
{

    /// <summary>
    /// 
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA1801:Review unused parameters", Justification = "<Pending>")]
    public record RestierApiModelMap(Type ApiType, IEdmModel Model);

}
