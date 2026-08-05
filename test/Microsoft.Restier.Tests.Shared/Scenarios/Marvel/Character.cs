// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.ObjectModel;

namespace Microsoft.Restier.Tests.Shared.Scenarios.Marvel
{

    /// <summary>
    /// 
    /// </summary>
    public class Character
    {

        public Guid Id { get; set; }

        public string Name { get; set; }

        public ObservableCollection<Comic> ComicsAppearedIn { get; set; }

        public ObservableCollection<Series> SeriesStarredIn { get; set; }

        public Character()
        {
            ComicsAppearedIn = new ObservableCollection<Comic>();
            SeriesStarredIn = new ObservableCollection<Series>();
        }

    }

}
