// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using System;
using System.Linq;

namespace Microsoft.Restier.AspNetCore.Model;

/// <summary>
/// Model mapper for Restier Web Api.
/// </summary>
public class RestierWebApiModelMapper : IModelMapper
{
    private readonly RestierWebApiModelExtender _modelExtender;

    /// <summary>
    /// Initializes a new instance of the <see cref="RestierWebApiModelMapper"/> class.
    /// </summary>
    /// <param name="modelExtender">The model extender.</param>
    public RestierWebApiModelMapper(RestierWebApiModelExtender modelExtender) => _modelExtender = modelExtender;

    /// <summary>
    /// Gets or sets the inner model mapper.
    /// </summary>
    public IModelMapper Inner { get; set; }

    /// <inheritdoc/>
    public bool TryGetRelevantType(InvocationContext context, string name, out Type relevantType)
    {
        if (Inner is not null &&
            Inner.TryGetRelevantType(context, name, out relevantType))
        {
            return true;
        }

        relevantType = null;
        var entitySetProperty = _modelExtender.EntitySetProperties.SingleOrDefault(p => p.Name == name);
        if (entitySetProperty is not null)
        {
            relevantType = entitySetProperty.PropertyType.GetGenericArguments()[0];
        }

        if (relevantType is null)
        {
            var singletonProperty = _modelExtender.SingletonProperties.SingleOrDefault(p => p.Name == name);
            if (singletonProperty is not null)
            {
                relevantType = singletonProperty.PropertyType;
            }
        }

        return relevantType is not null;
    }

    /// <inheritdoc/>
    public bool TryGetRelevantType(
        InvocationContext context,
        string namespaceName,
        string name,
        out Type relevantType)
    {
        if (Inner is not null &&
            Inner.TryGetRelevantType(context, namespaceName, name, out relevantType))
        {
            return true;
        }

        relevantType = null;
        return false;
    }
}