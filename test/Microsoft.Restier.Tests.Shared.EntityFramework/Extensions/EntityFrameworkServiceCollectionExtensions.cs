#if EF6
    using Microsoft.Restier.EntityFramework;
    using System;
    using System.Collections.Concurrent;
    using System.Data.Common;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    using System.Data.SqlClient;
    using System.Runtime.InteropServices;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Restier.Tests.Shared.Scenarios.Library.EF6;
    using Microsoft.Restier.Tests.Shared.Scenarios.Marvel.EF6;
#endif
#if EFCore
using System;
using System.Collections.Concurrent;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Restier.EntityFrameworkCore;
using Microsoft.Restier.EntityFrameworkCore.Spatial;
using Microsoft.Restier.Tests.Shared.EntityFrameworkCore;
using Microsoft.Restier.Tests.Shared.Scenarios.Library.EFCore;
using Microsoft.Restier.Tests.Shared.Scenarios.Library.EFCore.Views;
using Microsoft.Restier.Tests.Shared.Scenarios.Marvel.EFCore;
#endif

namespace Microsoft.Extensions.DependencyInjection
{
    public static class EFServiceCollectionExtensions
    {

#if EF6

        private static IConfiguration _configuration;
        private static readonly ConcurrentDictionary<string, object> DatabaseLocks = new();
        private static readonly ConcurrentDictionary<string, bool> InitializedDatabases = new();

        /// <summary>
        /// Gets the test configuration, loading user secrets if available.
        /// </summary>
        private static IConfiguration Configuration
        {
            get
            {
                if (_configuration is null)
                {
                    _configuration = new ConfigurationBuilder()
                        .AddUserSecrets(typeof(EFServiceCollectionExtensions).Assembly, optional: true)
                        .Build();
                }
                return _configuration;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="TDbContext"></typeparam>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddEntityFrameworkServices<TDbContext>(this IServiceCollection services) where TDbContext : DbContext
        {
            var connectionString = Configuration.GetConnectionString(typeof(TDbContext).Name);

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException($"Connection string 'ConnectionStrings:{typeof(TDbContext).Name}' is required. Add it with dotnet user-secrets.");
            }

            // Append the runtime version to the database name so that parallel TFM test runs
            // (e.g. net8.0 and net9.0) don't collide on the same database.
            var builder = new DbConnectionStringBuilder { ConnectionString = connectionString };
            if (builder.ContainsKey("Initial Catalog"))
            {
                builder["Initial Catalog"] = $"{builder["Initial Catalog"]}_{Environment.Version.Major}";
            }
            else if (builder.ContainsKey("Database"))
            {
                builder["Database"] = $"{builder["Database"]}_{Environment.Version.Major}";
            }

            services.AddEF6ProviderServices<TDbContext>(builder.ConnectionString);
            Microsoft.Restier.EntityFramework.Spatial.ServiceCollectionExtensions.AddRestierSpatial(services);

            // Ensure a clean, freshly-seeded database once per process. EF6's
            // DropCreateDatabaseIfModelChanges only wipes on a model-hash change, which lets
            // destructive tests (DeepUpdate / DeepInsert / Batch) accumulate state across
            // sessions and eventually corrupt seeded fixtures (Publisher1, "Jungle Book, The",
            // etc.). Mirror the EFCore SeedDatabase behavior so each `dotnet test` run starts
            // from the same known seed.
            SeedDatabase<TDbContext>(builder.ConnectionString);

            return services;
        }

        /// <summary>
        /// Drops and re-initializes the EF6 database for <typeparamref name="TContext"/> once per
        /// process per connection string. Relies on the initializer set in the context constructor
        /// (typically <see cref="DropCreateDatabaseIfModelChanges{TContext}"/>) to recreate the
        /// schema and run Seed.
        /// </summary>
        /// <remarks>
        /// Uses <c>ALTER DATABASE ... SET SINGLE_USER WITH ROLLBACK IMMEDIATE</c> to force-close
        /// any pooled connections (e.g. from a prior test run) before dropping. Without this,
        /// <c>SqlException: Cannot drop database "X" because it is currently in use</c> would
        /// surface on repeated runs against the same SQL Server instance — common on macOS
        /// where the Docker SQL Server stays alive between runs and connections persist in the
        /// pool. The force-close runs against <c>master</c> so it isn't blocked by our own
        /// target-DB connection.
        /// </remarks>
        private static void SeedDatabase<TContext>(string connectionString)
            where TContext : DbContext
        {
            var databaseLock = DatabaseLocks.GetOrAdd(connectionString, _ => new object());
            lock (databaseLock)
            {
                if (InitializedDatabases.ContainsKey(connectionString))
                {
                    return;
                }

                ForceDropDatabase(connectionString);

                using var context = (TContext)Activator.CreateInstance(typeof(TContext), connectionString);
                context.Database.Initialize(force: true);

                InitializedDatabases[connectionString] = true;
            }
        }

        /// <summary>
        /// Force-drops the database named in <paramref name="connectionString"/> if it exists.
        /// Connects to <c>master</c> and switches the target DB to SINGLE_USER WITH ROLLBACK
        /// IMMEDIATE so pooled connections from previous test runs are evicted before the DROP.
        /// No-op if the database does not exist.
        /// </summary>
        private static void ForceDropDatabase(string connectionString)
        {
            var sourceBuilder = new SqlConnectionStringBuilder(connectionString);
            var dbName = !string.IsNullOrEmpty(sourceBuilder.InitialCatalog)
                ? sourceBuilder.InitialCatalog
                : null;
            if (string.IsNullOrEmpty(dbName))
            {
                // Connection string has no target catalog — nothing to drop.
                return;
            }

            var masterBuilder = new SqlConnectionStringBuilder(connectionString)
            {
                InitialCatalog = "master",
            };

            using var connection = new SqlConnection(masterBuilder.ConnectionString);
            connection.Open();

            // Identifier injection guard: SQL Server database names allow brackets but must not
            // contain a closing bracket. Escape ] -> ]] inside the [...] form.
            var escaped = dbName.Replace("]", "]]");

            using var cmd = connection.CreateCommand();
            cmd.CommandText =
                $"IF DB_ID(N'{dbName.Replace("'", "''")}') IS NOT NULL " +
                $"BEGIN " +
                $"  ALTER DATABASE [{escaped}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; " +
                $"  DROP DATABASE [{escaped}]; " +
                $"END";
            cmd.ExecuteNonQuery();
        }

#endif

#if EFCore

        private static IConfiguration _configuration;
        private static readonly ConcurrentDictionary<string, object> DatabaseLocks = new();
        private static readonly ConcurrentDictionary<string, bool> InitializedDatabases = new();

        /// <summary>
        /// Gets the test configuration, loading user secrets if available.
        /// </summary>
        private static IConfiguration Configuration
        {
            get
            {
                if (_configuration is null)
                {
                    _configuration = new ConfigurationBuilder()
                        .AddUserSecrets(typeof(EFServiceCollectionExtensions).Assembly, optional: true)
                        .Build();
                }
                return _configuration;
            }
        }

        /// <summary>
        /// Adds Entity Framework Core provider services for the specified DbContext.
        /// Uses the SQL Server connection string configured in user secrets.
        /// </summary>
        /// <typeparam name="TDbContext">The type of the DbContext.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddEntityFrameworkServices<TDbContext>(this IServiceCollection services) where TDbContext : DbContext
        {
            var connectionString = Configuration.GetConnectionString(typeof(TDbContext).Name);

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException($"Connection string 'ConnectionStrings:{typeof(TDbContext).Name}' is required. Add it with dotnet user-secrets.");
            }

            var builder = new DbConnectionStringBuilder { ConnectionString = connectionString };
            if (builder.ContainsKey("Initial Catalog"))
            {
                builder["Initial Catalog"] = $"{builder["Initial Catalog"]}_{Environment.Version.Major}_EFCore";
            }
            else if (builder.ContainsKey("Database"))
            {
                builder["Database"] = $"{builder["Database"]}_{Environment.Version.Major}_EFCore";
            }

            services.AddEFCoreProviderServices<TDbContext>(options =>
                options.UseSqlServer(builder.ConnectionString, o => o.UseNetTopologySuite()));
            services.AddRestierSpatial();

            if (typeof(TDbContext) == typeof(LibraryContext))
            {
                services.SeedDatabase<LibraryContext, LibraryTestInitializer>();
            }
            else if (typeof(TDbContext) == typeof(MarvelContext))
            {
                services.SeedDatabase<MarvelContext, MarvelTestInitializer>();
            }
            else if (typeof(TDbContext) == typeof(LibraryWithViewsContext))
            {
                services.SeedDatabase<LibraryWithViewsContext, LibraryWithViewsTestInitializer>();
            }

            return services;
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="TContext"></typeparam>
        /// <typeparam name="TInitializer"></typeparam>
        /// <param name="services"></param>
        /// <returns></returns>
        public static void SeedDatabase<TContext, TInitializer>(this IServiceCollection services)
            where TContext : DbContext
            where TInitializer : IDatabaseInitializer, new()
        {
            using var tempServices = services.BuildServiceProvider();

            var scopeFactory = tempServices.GetService<IServiceScopeFactory>();
            using var scope = scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetService<TContext>();

            var databaseKey = dbContext.Database.IsRelational()
                ? dbContext.Database.GetConnectionString()
                : $"{dbContext.Database.ProviderName}:{typeof(TContext).FullName}";
            var databaseLock = DatabaseLocks.GetOrAdd(databaseKey, _ => new object());
            lock (databaseLock)
            {
                if (!InitializedDatabases.ContainsKey(databaseKey))
                {
                    dbContext.Database.EnsureDeleted();
                    dbContext.Database.EnsureCreated();

                    var initializer = new TInitializer();
                    initializer.Seed(dbContext);
                    InitializedDatabases[databaseKey] = true;
                }
            }

        }

#endif

    }

}
