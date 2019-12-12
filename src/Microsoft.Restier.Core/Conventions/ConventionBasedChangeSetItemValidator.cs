// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Restier.Core.Submit;

namespace Microsoft.Restier.Core
{
    /// <summary>
    /// A convention-based change set item validator.
    /// </summary>
    public class ConventionBasedChangeSetItemValidator :
        IChangeSetItemValidator
    {
        /// <inheritdoc/>
        public Task ValidateChangeSetItemAsync( SubmitContext context, ChangeSetItem item, Collection<ChangeSetItemValidationResult> validationResults, 
            CancellationToken cancellationToken)
        {
            Ensure.NotNull(validationResults, nameof(validationResults));
            if (item is DataModificationItem dataModificationItem)
            {
                var resource = dataModificationItem.Resource;

                // TODO GitHubIssue#50 : should this PropertyDescriptorCollection be cached?
                var properties = new AssociatedMetadataTypeTypeDescriptionProvider(resource.GetType())
                    .GetTypeDescriptor(resource).GetProperties();

                var validationContext = new ValidationContext(resource);

                foreach (PropertyDescriptor property in properties)
                {
                    validationContext.MemberName = property.Name;

                    var validationAttributes = property.Attributes.OfType<ValidationAttribute>();
                    foreach (var validationAttribute in validationAttributes)
                    {
                        var value = property.GetValue(resource);
                        var validationResult = validationAttribute.GetValidationResult(value, validationContext);
                        if (validationResult != ValidationResult.Success)
                        {
                            validationResults.Add(new ChangeSetItemValidationResult()
                            {
                                ValidatorType = validationAttribute.GetType().FullName,
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
