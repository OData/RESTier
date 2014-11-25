// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace System.Web.OData.Domain
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ActionAttribute : Attribute
    {
        public string Name { get; set; }
        
        public string Namespace { get; set; }
    }
}
