// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Microsoft.Restier.Tests.Shared.Scenarios.Annotated;

/// <summary>
/// Test entity exercising every attribute family the
/// ConventionBasedAnnotationModelBuilder is expected to translate.
/// </summary>
[Description("A widget — used by annotation integration tests.")]
public class AnnotatedEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Description("Database-assigned identifier.")]
    public int Id { get; set; }

    [Description("The display name of the widget.")]
    public string Name { get; set; }

    [ReadOnly(true)]
    [Description("UTC timestamp of when the widget was created.")]
    public DateTimeOffset CreatedOn { get; set; }

    [Range(0, 100)]
    [Description("Score between 0 and 100.")]
    public int Score { get; set; }

    [RegularExpression("^[A-Z]{2}$")]
    [Description("Two-letter country code.")]
    public string CountryCode { get; set; }
}
