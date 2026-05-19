// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using Microsoft.Restier.EntityFrameworkCore;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Microsoft.Restier.Tests.Shared.Scenarios.Library.EFCore;

namespace Microsoft.Restier.Tests.Shared.Scenarios.Library.EFCore.Views
{
    /// <summary>
    /// Thin LibraryApi-shaped class used by the keyless-view regression tests
    /// (<c>Issue741_KeylessViews</c>) to host an instrumented
    /// <see cref="OnFilteringBooksByPublisher" /> convention probe. The view itself lives on
    /// <see cref="LibraryContext" /> (added under <c>#if EFCore</c>); this class exists only so
    /// the probe doesn't pollute the widely-shared <c>LibraryApi</c> fixture.
    /// </summary>
    public class LibraryWithViewsApi : EntityFrameworkApi<LibraryContext>
    {
        /// <summary>
        /// Static counter incremented if/when the convention processor invokes this method.
        /// In v1 it stays at 0 (convention hooks do not fire for keyless-view function imports;
        /// see Follow-up A in the spec). Flipping this to "did fire" is the entry condition for
        /// the convention-processor follow-up.
        /// </summary>
        public static int OnFilteringBooksByPublisherCallCount;

        public LibraryWithViewsApi(LibraryContext dbContext, IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler)
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
