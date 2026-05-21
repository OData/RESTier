// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

#nullable disable

namespace Microsoft.Restier.Samples.Postgres.AspNetCore.Models
{
    /// <summary>
    /// Keyless view aggregating user count by user type. Demonstrates the
    /// auto-mapped keyless-view feature: surfaces in the EDM as a ComplexType +
    /// unbound FunctionImport, callable via <c>GET /v3/UsersByType()</c>.
    /// </summary>
    public class UsersByType
    {
        /// <summary>
        /// Gets or sets the display name of the user type (from <see cref="UserType.DisplayName"/>).
        /// </summary>
        public string TypeName { get; set; }

        /// <summary>
        /// Gets or sets the count of users associated with this user type.
        /// </summary>
        public int UserCount { get; set; }
    }
}
