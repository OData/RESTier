// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Restier.Core.Submit;
using DataAnnotations = System.ComponentModel.DataAnnotations;
using ValidationResult = Microsoft.Restier.Core.Submit.ValidationResult;

namespace Microsoft.Restier.Conventions
{
    /// <summary>
    /// A conventional change set entry validator.
    /// </summary>
    public class ConventionalChangeSetEntryValidator :
        IChangeSetEntryValidator
    {
        static ConventionalChangeSetEntryValidator()
        {
            Instance = new ConventionalChangeSetEntryValidator();
        }

        private ConventionalChangeSetEntryValidator()
        {
        }

        /// <summary>
        /// Gets a static instance of conventional change set entry validator.
        /// </summary>
        public static ConventionalChangeSetEntryValidator Instance { get; private set; }

        /// <inheritdoc/>
        public Task ValidateEntityAsync(
            SubmitContext context,
            ChangeSetEntry entry,
            ValidationResults validationResults,
            CancellationToken cancellationToken)
        {
            Ensure.NotNull(validationResults);
            DataModificationEntry dataModificationEntry = entry as DataModificationEntry;
            if (dataModificationEntry != null)
            {
                object entity = dataModificationEntry.Entity;

                // TODO GitHubIssue#50 : should this PropertyDescriptorCollection be cached?
                PropertyDescriptorCollection properties =
                    new AssociatedMetadataTypeTypeDescriptionProvider(entity.GetType())
                    .GetTypeDescriptor(entity).GetProperties();

                ValidationContext validationContext = new ValidationContext(entity);

                foreach (PropertyDescriptor property in properties)
                {
                    validationContext.MemberName = property.Name;

                    IEnumerable<ValidationAttribute> validationAttributes =
                        property.Attributes.OfType<ValidationAttribute>();
                    foreach (ValidationAttribute validationAttribute in validationAttributes)
                    {
                        object value = property.GetValue(entity);
                        DataAnnotations.ValidationResult validationResult =
                            validationAttribute.GetValidationResult(value, validationContext);
                        if (validationResult != DataAnnotations.ValidationResult.Success)
                        {
                            validationResults.Add(new ValidationResult()
                            {
                                Id = validationAttribute.GetType().FullName,
                                Message = validationResult.ErrorMessage,
                                Severity = ValidationSeverity.Error,
                                Target = entity,
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
