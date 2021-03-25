using System;
using Microsoft.OData.Edm;

namespace Microsoft.Restier.Core.Startup
{
    /// <summary>
    /// 
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA1801:Review unused parameters", Justification = "<Pending>")]
    public record RestierApiModelMap(Type ApiType, IEdmModel Model);

    /// <summary>
    /// 
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA1801:Review unused parameters", Justification = "<Pending>")]
    public record RestierRouteEntry(string RouteName, string RoutePrefix, Type ApiType, bool AllowBatching = true);


}
