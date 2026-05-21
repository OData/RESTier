// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Restier.AspNetCore;
using Microsoft.Restier.AspNetCore.NSwag;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Reflection;

namespace Microsoft.Restier.Tests.AspNetCore.NSwag
{

    [TestClass]
    public class RestierControllerApiExplorerConventionTests
    {

        [TestMethod]
        public void Apply_HidesRestierControllerActions_FromApiExplorer()
        {
            var convention = new RestierControllerApiExplorerConvention();
            var application = BuildApplicationModel(typeof(RestierController), typeof(SamplePlainController));

            convention.Apply(application);

            var restierActions = application.Controllers
                .Single(c => c.ControllerType.AsType() == typeof(RestierController))
                .Actions;
            restierActions.Should().AllSatisfy(a => a.ApiExplorer.IsVisible.Should().Be(false));
        }

        [TestMethod]
        public void Apply_LeavesNonRestierControllers_Untouched()
        {
            var convention = new RestierControllerApiExplorerConvention();
            var application = BuildApplicationModel(typeof(RestierController), typeof(SamplePlainController));

            convention.Apply(application);

            var plainActions = application.Controllers
                .Single(c => c.ControllerType.AsType() == typeof(SamplePlainController))
                .Actions;
            plainActions.Should().AllSatisfy(a => a.ApiExplorer.IsVisible.Should().NotBe(false),
                "convention must not change visibility on non-Restier controllers");
        }

        private static ApplicationModel BuildApplicationModel(params Type[] controllerTypes)
        {
            var application = new ApplicationModel();
            foreach (var t in controllerTypes)
            {
                var controllerInfo = t.GetTypeInfo();
                var controller = new ControllerModel(controllerInfo, controllerInfo.GetCustomAttributes(inherit: true).Cast<object>().ToArray());
                foreach (var method in t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    if (method.IsSpecialName) { continue; }
                    var action = new ActionModel(method, method.GetCustomAttributes(inherit: true).Cast<object>().ToArray());
                    controller.Actions.Add(action);
                }
                application.Controllers.Add(controller);
            }
            return application;
        }

        public class SamplePlainController : ControllerBase
        {
            public IActionResult Get() => new OkResult();
        }

    }

}
