// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Microsoft.Restier.Providers.InMemory.Utils
{
    public static class InMemoryProviderUtils
    {
        static public string GetSessionId()
        {
            var session = System.Web.HttpContext.Current.Session;
            if (session != null)
            {
                return session.SessionID;
            }

            return null;
        }
    }
}