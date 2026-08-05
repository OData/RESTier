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

        public string Id { get; set; }

        public Address Addr { get; set; }

        public DateTimeOffset LastUpdated { get; set; }

        public virtual ObservableCollection<Book> Books { get; set; }

        public Publisher()
        {
            Books = new ObservableCollection<Book>();
        }

    }

}