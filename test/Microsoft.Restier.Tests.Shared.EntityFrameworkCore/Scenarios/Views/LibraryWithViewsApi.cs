// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using Microsoft.Restier.EntityFrameworkCore;

namespace Microsoft.Restier.Tests.Shared.Scenarios.Library.EFCore.Views
{
    public class LibraryWithViewsApi : EntityFrameworkApi<LibraryWithViewsContext>
    {
        /// <summary>
        /// Static counter incremented when the convention processor invokes this method.
        /// In v1 it stays at 0; flipping when the follow-up lands will be a deliberate test change.
        /// </summary>
        public static int OnFilteringBooksByPublisherCallCount;

        public LibraryWithViewsApi(LibraryWithViewsContext dbContext, IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler)
            : base(dbContext, model, queryHandler, submitHandler)
        {
        }

        protected internal IQueryable<BooksByPublisher> OnFilteringBooksByPublisher(IQueryable<BooksByPublisher> entitySet)
        {
            System.Threading.Interlocked.Increment(ref OnFilteringBooksByPublisherCallCount);
            return entitySet;
        }
    }
}
