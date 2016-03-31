// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Microsoft.OData.Core;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Library;

namespace Microsoft.Restier.WebApi.Test.Services.Trippin.Models
{
    /// <summary>
    /// The customized payload value converter for test only
    /// </summary>
    public class CustomizedPayloadValueConverter : RestierPayloadValueConverter
    {
        public override object ConvertToPayloadValue(object value, IEdmTypeReference edmTypeReference)
        {
            if (edmTypeReference != null)
            {
                if (value is string)
                {
                    var stringValue = (string) value;

                    // Make People(1)/FirstName converted
                    if (stringValue == "Russell")
                    {
                        return stringValue + "Converter";
                    }
                }
            }

            return base.ConvertToPayloadValue(value, edmTypeReference);
        }
    }
}
