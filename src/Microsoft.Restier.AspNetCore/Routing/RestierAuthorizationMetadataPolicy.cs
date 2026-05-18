// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Matching;
using Microsoft.OData.UriParser;
using System;
using System.Linq;

namespace Microsoft.Restier.AspNetCore.Routing;

/// <summary>
/// A <see cref="MatcherPolicy"/> that augments the matched <see cref="Microsoft.AspNetCore.Http.Endpoint"/>
/// for a Restier route with any <see cref="Microsoft.AspNetCore.Authorization.IAuthorizeData"/> or
/// <see cref="Microsoft.AspNetCore.Authorization.IAllowAnonymous"/> attributes found on the user's
/// <see cref="Core.ApiBase"/> subclass, its <see cref="Model.ResourceAttribute"/>-decorated
/// properties, or its <see cref="Model.BoundOperationAttribute"/> /
/// <see cref="Model.UnboundOperationAttribute"/> methods.
/// </summary>
internal sealed class RestierAuthorizationMetadataPolicy : MatcherPolicy
{
    private const string ClassKey = "class";
    private const string ResourcePrefix = "resource:";
    private const string OperationPrefix = "operation:";

    /// <summary>
    /// Maps an <see cref="ODataPath"/> to a stable string key identifying the user-code target
    /// whose attributes should be honored: the class, a named resource property, or a named
    /// operation method. The key doubles as a cache key for the discovered attribute list.
    /// </summary>
    internal static string ComputeTargetKey(ODataPath path)
    {
        if (path is null || path.Count == 0)
        {
            return ClassKey;
        }

        var lastSegment = path.LastOrDefault();
        if (lastSegment is MetadataSegment)
        {
            return ClassKey;
        }

        // Operations win because they are the actual action being invoked. A bound operation
        // (path ending in OperationSegment) overrides the entity-set's attributes.
        if (lastSegment is OperationImportSegment opImport)
        {
            var op = opImport.OperationImports.FirstOrDefault();
            return op is null ? ClassKey : OperationPrefix + op.Name;
        }
        if (lastSegment is OperationSegment opSeg)
        {
            var op = opSeg.Operations.FirstOrDefault();
            return op is null ? ClassKey : OperationPrefix + op.Name;
        }

        // Otherwise the first segment identifies the resource the request targets.
        var firstSegment = path.FirstOrDefault();
        if (firstSegment is EntitySetSegment esSeg)
        {
            return ResourcePrefix + esSeg.EntitySet.Name;
        }
        if (firstSegment is SingletonSegment singletonSeg)
        {
            return ResourcePrefix + singletonSeg.Singleton.Name;
        }

        return ClassKey;
    }

    /// <inheritdoc/>
    // DynamicControllerEndpointMatcherPolicy.Order == int.MinValue + 100. We run after it so the
    // OData path is already parsed and the candidate endpoint is the RestierController action.
    public override int Order => int.MinValue + 110;
}
