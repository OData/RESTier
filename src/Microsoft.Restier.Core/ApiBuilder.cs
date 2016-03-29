// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Restier.Core
{
    public sealed class ApiBuilder
    {
        private static Action<IServiceCollection> emptyConfig = _ => { };

        private Action<IServiceCollection> inner, outer;

        [CLSCompliant(false)]
        public static Action<IServiceCollection> DefaultInnerMost
        {
            get { return ApiBuilderExtensions.DefaultInnerMost; }
        }

        [CLSCompliant(false)]
        public static Action<IServiceCollection> DefaultOuterMost
        {
            get { return ApiBuilderExtensions.DefaultOuterMost; }
        }

        [CLSCompliant(false)]
        public Action<IServiceCollection> Configuration
        {
            get { return (inner + outer) ?? emptyConfig; }
        }

        /// <summary>
        /// Adds a configuration procedure at the inner end.
        /// </summary>
        /// <param name="configurationCallback">
        /// An action that will be called when building the <see cref="ApiConfiguration"/>.
        /// </param>
        /// <returns>The <see cref="ApiBuilder"/>.</returns>
        [CLSCompliant(false)]
        public ApiBuilder AddInnerMost(Action<IServiceCollection> configurationCallback)
        {
            inner = configurationCallback + inner;
            return this;
        }

        [CLSCompliant(false)]
        public ApiBuilder AddInnerTail(Action<IServiceCollection> configurationCallback)
        {
            inner = inner + configurationCallback;
            return this;
        }

        [CLSCompliant(false)]
        public ApiBuilder AddOuterHead(Action<IServiceCollection> configurationCallback)
        {
            outer = configurationCallback + outer;
            return this;
        }

        [CLSCompliant(false)]
        public ApiBuilder AddOuterMost(Action<IServiceCollection> configurationCallback)
        {
            outer = outer + configurationCallback;
            return this;
        }

        [CLSCompliant(false)]
        public ApiConfiguration Build(
            Action<IServiceCollection> innerMost,
            Action<IServiceCollection> outerMost,
            IServiceCollection services,
            Func<IServiceCollection, IServiceProvider> serviceProviderFactory)
        {
            var configureCall = innerMost + Configuration + outerMost;
            configureCall(services);

            return services.BuildApiConfiguration(serviceProviderFactory);
        }
    }
}
