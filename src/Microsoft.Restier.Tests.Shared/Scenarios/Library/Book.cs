// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Restier.Tests.Shared.Scenarios.Library
{

    /// <summary>
    /// 
    /// </summary>
    public class Book
    {
        /// <summary>
        /// Without this property, EntityFramework will complain that this object doesn't have a key.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// A reference key to <see cref="Publisher"/> is required to support both EntityFramework and EntityFrameworkCore.
        /// </summary>
        public string PublisherId { get; set; }

        [MinLength(13)]
        [MaxLength(13)]
        public string Isbn { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public Publisher Publisher { get; set; }

    }
}