using System;
using System.Collections.ObjectModel;

namespace Microsoft.Restier.Tests.Shared.Scenarios.Marvel
{
    public class Series
    {

        public Guid Id { get; set; }

        public string DisplayName { get; set; }

        public ObservableCollection<Character> MainCharacters { get; set; }

        public ObservableCollection<Comic> Comics { get; set; }

        public Series()
        {
            Comics = new ObservableCollection<Comic>();
            MainCharacters = new ObservableCollection<Character>();
        }

    }

}
