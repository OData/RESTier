// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;

namespace Microsoft.Restier.Tests.Shared.Scenarios.Library
{

    /// <summary>
    /// 
    /// </summary>
    public class Employee
    {
        /// <summary>
        /// Without this property, EntityFramework will complain that this object doesn't have a key.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// A reference key to <see cref="Address"/> is required to support both EntityFramework and EntityFrameworkCore.
        /// </summary>
        public Guid AddrId { get; set; }

        /// <summary>
        /// A reference key to <see cref="Universe"/> is required to support both EntityFramework and EntityFrameworkCore.
        /// </summary>
        public Guid UniverseId { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string FullName { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public Address Addr { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public Universe Universe { get; set; }

    }
}