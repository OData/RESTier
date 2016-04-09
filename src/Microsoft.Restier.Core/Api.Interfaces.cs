// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Restier.Core
{
    [CLSCompliant(false)]
    public interface IApiConfigurator
    {
        void Configure(IServiceCollection services, Type apiType);
    }

    public interface IApiInitializer
    {
        void Initialize(ApiConfiguration configuration);
    }

    public interface IApiContextConfigurator
    {
        void Initialize(ApiContext context);

        void Cleanup(ApiContext context);
    }

    [CLSCompliant(false)]
    public interface IApiContextFactory
    {
        ApiContext CreateWithin(IServiceScope scope);
    }
}
