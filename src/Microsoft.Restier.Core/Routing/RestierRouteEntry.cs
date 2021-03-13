using System;
using System.ComponentModel;


namespace Microsoft.Restier.Core.Routing
{

    /// <summary>
    /// 
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA1801:Review unused parameters", Justification = "<Pending>")]
    public record RestierRouteEntry(string RouteName, string RoutePrefix, Type ApiType, bool AllowBatching = true);

}
