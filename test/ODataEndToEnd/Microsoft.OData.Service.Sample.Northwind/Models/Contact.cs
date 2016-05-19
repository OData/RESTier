// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Microsoft.OData.Service.Sample.Northwind.Models
{
    public partial class Contact
    {
        public int ContactID { get; set; }

        [StringLength(50)]
        public string ContactType { get; set; }

        [Required]
        [StringLength(40)]
        public string CompanyName { get; set; }

        [StringLength(30)]
        public string ContactName { get; set; }

        [StringLength(30)]
        public string ContactTitle { get; set; }

        [StringLength(60)]
        public string Address { get; set; }

        [StringLength(15)]
        public string City { get; set; }

        [StringLength(15)]
        public string Region { get; set; }

        [StringLength(10)]
        public string PostalCode { get; set; }

        [StringLength(15)]
        public string CountryRegion { get; set; }

        [StringLength(24)]
        public string Phone { get; set; }

        [StringLength(4)]
        public string Extension { get; set; }

        [StringLength(24)]
        public string Fax { get; set; }

        [Column(TypeName = "ntext")]
        public string HomePage { get; set; }

        [StringLength(255)]
        public string PhotoPath { get; set; }

        [Column(TypeName = "image")]
        public byte[] Photo { get; set; }
    }
}
