// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.OData.Edm;
using Microsoft.Restier.Publishers.OData;

namespace Microsoft.OData.Service.Sample.Trippin.Models
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
