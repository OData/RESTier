﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Core;

namespace System
{

    /// <summary>
    /// 
    /// </summary>
    public static class IServiceProviderExtensions
    {

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T GetTestableApiInstance<T>(this IServiceProvider serviceProvider) 
            where T : ApiBase => serviceProvider.GetService<T>();

    }

}