// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Restier.Core.DependencyInjection
{
    /// <summary>
    /// Interface implemented by services that are chained 
    /// together to form a chain of responsibility.
    /// </summary>
    /// <typeparam name="TService">The type of the service</typeparam>
    public interface IChainedService<TService>
        where TService : class
    {
        /// <summary>
        /// Gets a reference to an inner service in case they are chained.
        /// </summary>
        TService Inner { get; set; }
    }
}
