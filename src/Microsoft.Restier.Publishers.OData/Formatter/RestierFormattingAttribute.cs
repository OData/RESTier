// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http.Controllers;
using System.Web.OData.Formatter;

namespace Microsoft.Restier.Publishers.OData.Formatter
{
    /// <summary>
    /// Specifies the serializer and deserializer provider for the API controller.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    internal sealed class RestierFormattingAttribute : Attribute, IControllerConfiguration
    {
        /// <summary>
        /// Inserts the RESTier specific formatters to the controller.
        /// </summary>
        /// <param name="controllerSettings">The controller settings.</param>
        /// <param name="controllerDescriptor">The controller descriptor.</param>
        public void Initialize(HttpControllerSettings controllerSettings, HttpControllerDescriptor controllerDescriptor)
        {
            Ensure.NotNull(controllerSettings, "controllerSettings");
            var controllerFormatters = controllerSettings.Formatters;
            IList<ODataMediaTypeFormatter> odataFormatters =
                controllerFormatters.OfType<ODataMediaTypeFormatter>().ToList();
            if (!odataFormatters.Any())
            {
                foreach (var formatter in odataFormatters)
                {
                    controllerFormatters.Remove(formatter);
                }
            }

            odataFormatters = ODataMediaTypeFormatters.Create(
                new RestierSerializerProviderProxy(),
                new RestierDeserializerProviderProxy());
            controllerFormatters.InsertRange(0, odataFormatters);
        }
    }
}
