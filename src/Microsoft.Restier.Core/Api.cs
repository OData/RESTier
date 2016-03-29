// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Restier.Core
{
    public sealed class Api<TKey> where TKey : class
    {
        private static Lazy<ApiConfiguration> apiConfiguration =
            new Lazy<ApiConfiguration>(BuildConfiguration, LazyThreadSafetyMode.ExecutionAndPublication);

        public static ApiConfiguration Configuration
        {
            get { return apiConfiguration.Value; }
        }

        public static ApiBuilder Configure()
        {
            return ApiConfiguration.Configure<TKey>();
        }

        private static ApiConfiguration BuildConfiguration()
        {
            return ApiConfiguration.Configure<TKey>().Build();
        }
    }
}
