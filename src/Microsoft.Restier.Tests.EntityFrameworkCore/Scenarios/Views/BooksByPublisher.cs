#if NET6_0_OR_GREATER

using Microsoft.EntityFrameworkCore;

namespace Microsoft.Restier.Tests.EntityFrameworkCore.Scenarios.Views
{

    [Keyless]
    public partial class BooksByPublisher
    {

        public int PublisherId { get; set; }

        public string PublisherName { get; set; }

        public string BookName { get; set; }

        public decimal BookCount { get; set; }

    }

}

#endif