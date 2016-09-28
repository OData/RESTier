// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Microsoft.Restier.Core
{
    internal static class ConventionBasedChangeSetConstants
    {
#region Authorizing method constants
        public const string AuthorizeMethodDataModificationInsert = "CanInsert";

        public const string AuthorizeMethodDataModificationUpdate = "CanUpdate";

        public const string AuthorizeMethodDataModificationDelete = "CanDelete";

        public const string AuthorizeMethodActionInvocationExecute = "CanExecute";
#endregion

#region Filtering method constants
        public const string FilterMethodDataModificationInsert = "OnInsert";

        public const string FilterMethodDataModificationUpdate = "OnUpdat";

        public const string FilterMethodDataModificationDelete = "OnDelet";

        public const string FilterMethodActionInvocationExecute = "OnExecut";

        public const string FilterMethodEntitySetFilter = "OnFilter";

        public const string FilterMethodNamePreFilterSuffix = "ing";

        public const string FilterMethodNamePostFilterSuffix = "ed";
#endregion
    }
}
