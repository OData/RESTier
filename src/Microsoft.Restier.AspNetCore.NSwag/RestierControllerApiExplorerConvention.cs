// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Restier.AspNetCore;
using System;

namespace Microsoft.Restier.AspNetCore.NSwag
{

    /// <summary>
    /// MVC application-model convention that hides <see cref="RestierController"/> actions from
    /// ApiExplorer. Any OpenAPI generator that relies on ApiExplorer (NSwag, Swashbuckle, .NET 9
    /// OpenAPI) will then exclude Restier endpoints from MVC-derived documents, so they cannot
    /// leak into a user's plain-controllers OpenAPI doc.
    /// </summary>
    internal class RestierControllerApiExplorerConvention : IApplicationModelConvention
    {

        public void Apply(ApplicationModel application)
        {
            if (application is null)
            {
                throw new ArgumentNullException(nameof(application));
            }

            foreach (var controller in application.Controllers)
            {
                if (!typeof(RestierController).IsAssignableFrom(controller.ControllerType))
                {
                    continue;
                }

                foreach (var action in controller.Actions)
                {
                    action.ApiExplorer.IsVisible = false;
                }
            }
        }

    }

}
