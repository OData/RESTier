// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Microsoft.Restier.Tests.Shared.Scenarios.Library
{

    /// <summary>
    /// CLR shape of the BooksByPublisher SQL view. Used by the EFCore LibraryContext as a keyless
    /// entity (mapped via fluent <c>HasNoKey().ToView("BooksByPublisher")</c>) so Restier surfaces
    /// it as a <c>ComplexType</c> + unbound <c>FunctionImport</c> per the keyless-views feature
    /// (issue #741). No EF attribute on the class itself so the type is TFM-agnostic.
    /// </summary>
    public partial class BooksByPublisher
    {

        // Publisher.Id is a string in the shared Library fixture (e.g. "Publisher1").
        public string PublisherId { get; set; }

        public string BookName { get; set; }

        public int BookCount { get; set; }

    }

}
