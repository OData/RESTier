// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using Microsoft.OData;

namespace Microsoft.AspNet.OData
{
    /// <summary>
    /// A set of <see cref="PerRouteContainer"/> extension methods to help ensure the RestierContainerBuilder is built with the correct
    /// services for the given Route.
    /// </summary>
    /// <remarks>
    /// This method uses Reflection wherever possible to ensure that changes to the default OData services for the container are always picked up.
    /// </remarks>
    internal static class PerRouteContainerExtensions
    {
        /// <summary>
        /// Create a root container for a given route name.
        /// </summary>
        /// <param name="prc">The <see cref="PerRouteContainer"/> instance to enhance.</param>
        /// <param name="routeName">The route name.</param>
        /// <param name="internalAction">The configuration actions to apply to the container.</param>
        /// <param name="developerAction">The configuration actions to apply to the container.</param>
        /// <returns>An instance of <see cref="IServiceProvider"/> to manage services for a route.</returns>
        internal static IServiceProvider CreateODataRouteContainer(this PerRouteContainer prc, string routeName, Action<IContainerBuilder> internalAction, Action<IContainerBuilder, string> developerAction)
        {
            if (prc == null)
            {
                throw new ArgumentNullException(nameof(prc));
            }

            var coreServicesMethod = prc.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault(c => c.Name == "CreateContainerBuilderWithCoreServices");
            var builder = (IContainerBuilder)coreServicesMethod.Invoke(prc, null);

            //RWM: This method invokes OData's builder actions, which are added to the container first.
            internalAction?.Invoke(builder);

            //RWM: This method invokes the developer's builder actions and passes in the route to let Restier add specific services for specific routes.
            developerAction?.Invoke(builder, routeName);

            var rootContainer = builder.BuildContainer();
            if (rootContainer == null)
            {
                throw new Exception("The container returned by BuildContainer was null. Please check the registered ContainerBuidler and try again.");
            }

            var setContainerMethod = prc.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault(c => c.Name == "SetContainer");
            setContainerMethod.Invoke(prc, new object[] { routeName, rootContainer });

            return rootContainer;
        }
    }
}
