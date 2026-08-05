// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.OData.Edm;
using System;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;

namespace Microsoft.Restier.Tests.Shared
{
    /// <summary>
    /// 
    /// </summary>
    public class StoreApi : ApiBase
    {
        public StoreApi(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler) : base(model, queryHandler, submitHandler)
        {

        }
    }
}