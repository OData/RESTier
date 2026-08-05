// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

#if EF6
    using System.Data.Entity;
#else
    using Microsoft.EntityFrameworkCore;
#endif

using Microsoft.Restier.Tests.Shared.Scenarios.Marvel;

#if EF6
namespace Microsoft.Restier.Tests.Shared.Scenarios.Marvel.EF6
#elif EFCore
namespace Microsoft.Restier.Tests.Shared.Scenarios.Marvel.EFCore
#endif
{

    /// <summary>
    /// The data context for the Marvel scenario.
    /// </summary>
    public class MarvelContext : DbContext
    {

#if EF6

        #region EntitySet Properties

        public IDbSet<Character> Characters { get; set; }

        public IDbSet<Comic> Comics { get; set; }

        public IDbSet<Series> Series { get; set; }

        #endregion

        public MarvelContext()
            : base("MarvelContext") => Database.SetInitializer(new MarvelTestInitializer());

        /// <summary>
        /// Creates a new instance with an explicit connection string.
        /// </summary>
        /// <param name="connectionString">The connection string to use.</param>
        public MarvelContext(string connectionString)
            : base(connectionString) => Database.SetInitializer(new MarvelTestInitializer());

#else

        #region EntitySet Properties

        public DbSet<Character> Characters { get; set; }

        public DbSet<Comic> Comics { get; set; }

        public DbSet<Series> Series { get; set; }

        #endregion

        public MarvelContext(DbContextOptions<MarvelContext> options) : base(options)
        {
        }

#endif

    }

}
