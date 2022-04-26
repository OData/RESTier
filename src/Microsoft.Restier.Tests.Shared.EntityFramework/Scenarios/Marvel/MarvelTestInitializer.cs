// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.ObjectModel;
#if EF6
using System.Data.Entity;
#endif
#if EFCore
using Microsoft.EntityFrameworkCore;
using Microsoft.Restier.Tests.Shared.EntityFrameworkCore;
#endif

namespace Microsoft.Restier.Tests.Shared.Scenarios.Marvel
{

    public class MarvelTestInitializer
#if EF6
        : DropCreateDatabaseAlways<MarvelContext>
    {

        protected override void Seed(MarvelContext context)
        {
            context.Comics.Add(new Comic
#else
        : IDatabaseInitializer
    {

        public void Seed(DbContext context)
        {
            (context as MarvelContext).Comics.Add(new Comic
#endif

            {
                Id = new Guid("C64BFB73-74C0-4C5E-9DD9-3D102D821461"),
                Isbn = "1234567890123",
                DisplayName = "Tales of Suspense #39",
                IssueNumber = 39,
                Characters = new ObservableCollection<Character>
                {
                    new Character
                    {
                        Id = new Guid("398DF851-3E3B-41A6-AC63-DE7E73711E71"),
                        Name = "Iron Man",
                        SeriesStarredIn = new ObservableCollection<Series>
                        {
                            new Series
                            {
                                Id = new Guid("77A5345D-17EA-4DB6-924C-568E7D7C8788"),
                                DisplayName = "Iron Man"
                            }
                        }
                    }
                },
                Series = new Series
                {
                    Id = new Guid("DB23A62B-55FC-4310-ACD5-8FC04F2AA355"),
                    DisplayName = "Tales of Suspense"
                }
            });

            context.SaveChanges();
        }

    }

}
