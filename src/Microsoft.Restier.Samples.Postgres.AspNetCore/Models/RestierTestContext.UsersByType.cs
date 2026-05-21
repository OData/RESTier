// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

#nullable disable

using Microsoft.EntityFrameworkCore;

namespace Microsoft.Restier.Samples.Postgres.AspNetCore.Models
{
    /// <summary>
    /// Adds the <see cref="UsersByType"/> keyless view to the context. The view's
    /// EF Core mapping (<c>HasNoKey().ToView("UsersByType")</c>) lives in the
    /// partial <c>OnModelCreatingPartial</c> body in <c>RestierTestContext.SeedData.cs</c>
    /// because C# allows only one implementing partial per partial method.
    /// </summary>
    public partial class RestierTestContext
    {
        /// <summary>
        /// Gets or sets the keyless view aggregating user count by user type.
        /// </summary>
        public virtual DbSet<UsersByType> UsersByType { get; set; }
    }
}
