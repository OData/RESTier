// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Restier.Samples.Postgres.AspNetCore.Models
{
    public class RestierTestContextFactory : IDesignTimeDbContextFactory<RestierTestContext>
    {
        public RestierTestContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddUserSecrets<RestierTestContextFactory>()
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<RestierTestContext>();
            optionsBuilder.UseNpgsql(configuration.GetConnectionString("RestierTestContext"), o => o.UseNetTopologySuite());

            return new RestierTestContext(optionsBuilder.Options);
        }
    }
}
