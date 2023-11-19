// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;

#if NET6_0_OR_GREATER
namespace Microsoft.Restier.AspNetCore.Model
#else
namespace Microsoft.Restier.AspNet.Model
#endif
{

    /// <summary>
    /// Attribute that indicates a property is an entity set or singleton.
    /// If the property type is IQueryable, it will be built as entity set or it will be built as singleton.
    /// The name will be same as property name.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class ResourceAttribute : Attribute
    {
    }

}