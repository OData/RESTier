// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.Restier.Tests.Shared.EntityFrameworkCore;
using Microsoft.Restier.Tests.Shared.Scenarios.Library.EFCore;

namespace Microsoft.Restier.Tests.Shared.Scenarios.Library.EFCore.Views
{
    /// <summary>
    /// Reuses LibraryTestInitializer to populate publishers/books, then creates the
    /// BooksByPublisher SQL view on top of the seeded data.
    /// </summary>
    public class LibraryWithViewsTestInitializer : IDatabaseInitializer
    {
        public void Seed(DbContext dbContext)
        {
            new LibraryTestInitializer().Seed(dbContext);

            dbContext.Database.ExecuteSqlRaw(@"
                IF OBJECT_ID('BooksByPublisher', 'V') IS NOT NULL DROP VIEW BooksByPublisher;
                EXEC('CREATE VIEW BooksByPublisher AS
                       SELECT p.Id AS PublisherId,
                              b.Title AS BookName,
                              CAST(COUNT(b.Id) OVER(PARTITION BY p.Id) AS INT) AS BookCount
                       FROM Publishers p
                       INNER JOIN Books b ON b.PublisherId = p.Id;');
            ");
        }
    }
}
