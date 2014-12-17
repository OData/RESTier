// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.OData.Domain.Test.Services.Trippin.Models;
using System.Web.Routing;

namespace System.Web.OData.Domain.Test.Services.Trippin
{
    public class WebApiApplication : HttpApplication
    {
        protected void Application_Start()
        {
            // Please run this drop-and-create for TrippinModel DB whenever model changes:
            // Database.SetInitializer(new DropCreateDatabaseAlways<TrippinModel>());
            GlobalConfiguration.Configure(WebApiConfig.Register);
        }
    }
}
