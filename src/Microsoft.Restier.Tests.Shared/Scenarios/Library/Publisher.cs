// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.Restier.Tests.Shared.Scenarios.Library
{

    /// <summary>
    /// 
    /// </summary>
    public class Publisher
    {
        /// <summary>
        /// Without this property, EntityFramework will complain that this object doesn't have a key.
        /// </summary>
        public string Id { get; set; }

        public string Name { get; set; }

        /// <summary>
        /// A reference key to <see cref="Address"/> is required to support both EntityFramework and EntityFrameworkCore.
        /// </summary>
        public Guid AddrId { get; set; }

        public Address Addr { get; set; }

        public virtual ObservableCollection<Book> Books { get; set; }

        public Publisher()
        {
            Books = new ObservableCollection<Book>();
        }

    }
}
