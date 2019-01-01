﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace Microsoft.Restier.Core
{
    internal static class ChainedService<TService> where TService : class
    {
        public static readonly Func<IServiceProvider, TService> DefaultFactory = sp =>
        {
            var instances = sp.GetServices<ApiServiceContributor<TService>>().Reverse();

            using (var e = instances.GetEnumerator())
            {
                Func<TService> next = null;
                next = () =>
                {
                    if (e.MoveNext())
                    {
                        return e.Current(sp, next);
                    }

                    return null;
                };

                return next();
            }
        };
    }
}
