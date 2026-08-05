// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Microsoft.Restier.AspNetCore.Model
{
    /// <summary>
    /// Defines the type of OData Operations that can be registered. The type of operation determines how the service
    /// responds over HTTP.
    /// </summary>
    public enum OperationType
    {

        /// <summary>
        /// Functions usually retrieve data from the system, and respond to requests made over HTTP GET.
        /// </summary>
        Function = 0,

        /// <summary>
        /// Actions usually submit data to the system, and respond to requests made over HTTP POST.
        /// </summary>
        Action = 1,
    }
}