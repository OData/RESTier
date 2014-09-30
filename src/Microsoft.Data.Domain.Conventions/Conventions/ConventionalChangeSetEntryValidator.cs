// Domain Framework ver. 1.0
// Copyright (c) Microsoft Corporation
// All rights reserved.
// MIT License
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
// associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute,
// sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial
// portions of the Software.
// 
// THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
// NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES
// OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Domain.Submit;
using DataAnnotations = System.ComponentModel.DataAnnotations;

namespace Microsoft.Data.Domain.Conventions
{
    /// <summary>
    /// A conventional change set entry validator.
    /// </summary>
    public class ConventionalChangeSetEntryValidator :
        IChangeSetEntryValidator
    {
        private ConventionalChangeSetEntryValidator()
        {
        }

        /// <summary>
        /// A static instance of conventional change set entry validator.
        /// </summary>
        public static readonly ConventionalChangeSetEntryValidator Instance =
            new ConventionalChangeSetEntryValidator();

        /// <inheritdoc/>
        public Task ValidateEntityAsync(
            SubmitContext context, ChangeSetEntry entry,
            ValidationResults validationResults,
            CancellationToken cancellationToken)
        {
            DataModificationEntry dataModificationEntry = entry as DataModificationEntry;
            if (dataModificationEntry != null)
            {
                object entity = dataModificationEntry.Entity;

                // TODO: should this PropertyDescriptorCollection be cached?
                PropertyDescriptorCollection properties = new AssociatedMetadataTypeTypeDescriptionProvider(entity.GetType())
                    .GetTypeDescriptor(entity).GetProperties();

                ValidationContext validationContext = new ValidationContext(entity);

                foreach (PropertyDescriptor property in properties)
                {
                    validationContext.MemberName = property.Name;

                    foreach (ValidationAttribute validationAttribute in property.Attributes.OfType<ValidationAttribute>())
                    {
                        object value = property.GetValue(entity);
                        DataAnnotations.ValidationResult validationResult = validationAttribute.GetValidationResult(value, validationContext);
                        if (validationResult != DataAnnotations.ValidationResult.Success)
                        {
                            validationResults.Add(new Submit.ValidationResult()
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
