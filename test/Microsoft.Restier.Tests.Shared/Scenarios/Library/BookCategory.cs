// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Text.Json.Serialization;

namespace Microsoft.Restier.Tests.Shared.Scenarios.Library
{
    /// <summary>
    /// Category of a book.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum BookCategory
    {
        Fiction = 0,
        NonFiction = 1,
        Science = 2,
    }
}
