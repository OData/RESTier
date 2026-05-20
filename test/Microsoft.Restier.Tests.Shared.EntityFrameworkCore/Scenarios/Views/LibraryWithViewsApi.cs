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
    /// <see cref="OnFilterBooksByPublisher" /> convention probe. The view itself lives on
    /// <see cref="LibraryContext" /> (added under <c>#if EFCore</c>); this class exists only so
    /// the probe doesn't pollute the widely-shared <c>LibraryApi</c> fixture.
    /// </summary>
    public class LibraryWithViewsApi : EntityFrameworkApi<LibraryContext>
    {
        /// <summary>
        /// Static counter incremented when the convention processor invokes this method.
        /// Follow-up A routes keyless-view function imports through the query pipeline, so
        /// the canonical convention name <c>OnFilter&lt;View&gt;</c> (no gerund — matches
        /// <c>GetFunctionImportMethodName</c> and the entity-set <c>OnFilter&lt;EntitySet&gt;</c>
        /// contract) now fires for keyless-view GETs.
        /// </summary>
        public static int OnFilterBooksByPublisherCallCount;

        public LibraryWithViewsApi(LibraryContext dbContext, IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler)
            : base(dbContext, model, queryHandler, submitHandler)
        {
        }

        /// <summary>
        /// Convention-based filter probe for the BooksByPublisher keyless view. Increments
        /// <see cref="OnFilterBooksByPublisherCallCount"/> so the regression suite can prove the
        /// convention fired, and filters out <c>Publisher3</c> rows so the convention's effect is
        /// observable in the HTTP response body (distinguishing provider-side composition from a
        /// no-op pipeline pass).
        /// </summary>
        protected internal IQueryable<BooksByPublisher> OnFilterBooksByPublisher(IQueryable<BooksByPublisher> entitySet)
        {
            System.Threading.Interlocked.Increment(ref OnFilterBooksByPublisherCallCount);
            return entitySet.Where(b => b.PublisherId != "Publisher3");
        }
    }
}
