// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Restier.Core.Submit;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Restier.Core
{
    /// <summary>
    /// A convention-based change set item validator.
    /// </summary>
    internal class ConventionBasedChangeSetItemValidator :
        IChangeSetItemValidator
    {
        /// <inheritdoc/>
        public Task ValidateChangeSetItemAsync(
            SubmitContext context,
            ChangeSetItem item,
            Collection<ChangeSetItemValidationResult> validationResults,
            CancellationToken cancellationToken)
        {
            Ensure.NotNull(validationResults, "validationResults");
            DataModificationItem dataModificationItem = item as DataModificationItem;
            if (dataModificationItem != null)
            {
                object resource = dataModificationItem.Resource;

                // TODO GitHubIssue#50 : should this PropertyDescriptorCollection be cached?
                PropertyDescriptorCollection properties =
                    new AssociatedMetadataTypeTypeDescriptionProvider(resource.GetType())
                    .GetTypeDescriptor(resource).GetProperties();

                ValidationContext validationContext = new ValidationContext(resource);

                foreach (PropertyDescriptor property in properties)
                {
                    validationContext.MemberName = property.Name;

                    IEnumerable<ValidationAttribute> validationAttributes =
                        property.Attributes.OfType<ValidationAttribute>();
                    foreach (ValidationAttribute validationAttribute in validationAttributes)
                    {
                        object value = property.GetValue(resource);
                        ValidationResult validationResult =
                            validationAttribute.GetValidationResult(value, validationContext);
                        if (validationResult != ValidationResult.Success)
                        {
                            validationResults.Add(new ChangeSetItemValidationResult()
                            {
                                Id = validationAttribute.GetType().FullName,
                                Message = validationResult.ErrorMessage,
                                Severity = EventLevel.Error,
                                Target = resource,
                                PropertyName = property.Name
                            });
                        }
                    }
                }
            }

            return Task.WhenAll();
        }
    }
}
