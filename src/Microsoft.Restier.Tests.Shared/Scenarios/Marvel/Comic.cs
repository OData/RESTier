// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Restier.Tests.Shared.Scenarios.Marvel
{
    public class Comic
    {

        public Guid Id { get; set; }

        [MinLength(13)]
        [MaxLength(13)]
        public string Isbn { get; set; }

        public string DisplayName { get; set; }

        public int IssueNumber { get; set; }

        public virtual ObservableCollection<Character> Characters { get; set; }

        public Series Series { get; set; }

        public Comic()
        {
            Characters = new ObservableCollection<Character>();
        }

    }

}
