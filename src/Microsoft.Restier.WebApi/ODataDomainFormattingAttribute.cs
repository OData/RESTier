// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http.Controllers;
using System.Web.OData.Formatter;
using System.Web.OData.Formatter.Deserialization;
using Microsoft.Restier.WebApi.Formatter.Serialization;

namespace Microsoft.Restier.WebApi
{
    /// <summary>
    /// Specifies the serializer and deserializer provider for the domain controller.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class ODataDomainFormattingAttribute : Attribute, IControllerConfiguration
    {
        /// <summary>
        /// Inserts the RESTier specific formatters to the controller.
        /// </summary>
        /// <param name="controllerSettings">The controller settings.</param>
        /// <param name="controllerDescriptor">The controller descriptor.</param>
        public void Initialize(HttpControllerSettings controllerSettings, HttpControllerDescriptor controllerDescriptor)
        {
            Ensure.NotNull(controllerSettings);
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
                new DefaultODataDomainSerializerProvider(),
                new DefaultODataDeserializerProvider());
            controllerFormatters.InsertRange(0, odataFormatters);
        }
    }
}
