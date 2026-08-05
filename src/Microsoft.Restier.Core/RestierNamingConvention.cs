// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Microsoft.Restier.Core
{
    /// <summary>
    /// Specifies the naming convention for OData JSON property names.
    /// </summary>
    public enum RestierNamingConvention
    {
        /// <summary>
        /// Use PascalCase property names (default). Property names match CLR type definitions.
        /// </summary>
        PascalCase = 0,

        /// <summary>
        /// Use lower camelCase property names. E.g. <c>FirstName</c> becomes <c>firstName</c>.
        /// </summary>
        LowerCamelCase = 1,

        /// <summary>
        /// Use lower camelCase for both property names and enum member names.
        /// </summary>
        LowerCamelCaseWithEnumMembers = 2,
    }
}
