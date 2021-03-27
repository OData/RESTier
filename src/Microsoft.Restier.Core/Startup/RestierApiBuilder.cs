// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Restier.Core
{

    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// The implementation of adding specific APIs is left to the implementing Web framework, either in ASP.NET or ASP.NET Core.
    /// The reason being that adding APIs requires Web runtime-speicific services that the Restier Core library cannot be not aware of.
    /// </remarks>
    public class RestierApiBuilder
    {

        #region Internal Properties

        /// <summary>
        /// The holder for all API registrations, keyed off the API type, with a value being an <see cref="Action{IServiceCollection}"/> 
        /// to add extra services to that particular API.
        /// </summary>
        internal Dictionary<Type, Action<IServiceCollection>> Apis { get; private set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new <see cref="RestierApiBuilder"/> instance.
        /// </summary>
        public RestierApiBuilder()
        {
            Apis = new();
        }

        #endregion

    }

}