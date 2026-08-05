// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Restier.Tests.Shared.Scenarios.Library
{

    /// <summary>
    ///
    /// </summary>
    public class Book
    {

        /// <summary>
        ///
        /// </summary>
        public Guid Id { get; set; }

        [MinLength(13)]
        [MaxLength(13)]
        public string Isbn { get; set; }

        /// <summary>
        ///
        /// </summary>
        public string Title { get; set; }

        public string PublisherId { get; set; }

        /// <summary>
        ///
        /// </summary>
        public Publisher Publisher { get; set; }

        public virtual ObservableCollection<Review> Reviews { get; set; }

        /// <summary>
        ///
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// The category of the book.
        /// </summary>
        public BookCategory? Category { get; set; }

        /// <summary>
        /// The date the book was published. CLR <see cref="DateTime"/> (not <see cref="DateTimeOffset"/>) so
        /// regression tests can verify the <see cref="DateTimeKind"/> produced by the OData $filter binder —
        /// see https://github.com/OData/RESTier/issues/704. Nullable so payloads that omit the field
        /// don't end up with <see cref="DateTime.MinValue"/> (Kind=Unspecified), which the OData
        /// DateTimeOffset deserializer rejects.
        /// </summary>
        public DateTime? PublishDate { get; set; }

        public Book()
        {
            Reviews = new ObservableCollection<Review>();
        }

    }

}