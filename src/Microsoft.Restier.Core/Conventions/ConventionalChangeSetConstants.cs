// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Microsoft.Restier.Core.Conventions
{
    internal static class ConventionalChangeSetConstants
    {
#region Authorizing method constants
        public const string AuthorizeMethodNamePrefix = "Can";

        public const string AuthorizeMethodDataModificationInsert = "Insert";

        public const string AuthorizeMethodDataModificationUpdate = "Update";

        public const string AuthorizeMethodDataModificationDelete = "Delete";

        public const string AuthorizeMethodActionInvocationExecute = "Execute";
#endregion

#region Filtering method constants
        public const string FilterMethodNamePrefix = "On";

        public const string FilterMethodDataModificationInsert = "Insert";

        public const string FilterMethodDataModificationUpdate = "Updat";

        public const string FilterMethodDataModificationDelete = "Delet";

        public const string FilterMethodActionInvocationExecute = "Execut";

        public const string FilterMethodEntitySetFilter = "Filter";

        public const string FilterMethodNamePreFilterSuffix = "ing";

        public const string FilterMethodNamePostFilterSuffix = "ed";
#endregion
    }
}
