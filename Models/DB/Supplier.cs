using System;
using System.Collections.Generic;
using System.Text;

namespace Fjord1.Int.API.Models.DB
{
    public class Supplier
    {
        public string SupplierNumber { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string TaxRegistrationNumber { get; set; }
        public string OrganizationNumber { get; set; }
        public string Street { get; set; }
        public string PostalCode { get; set; }
        public string City { get; set; }
        public string CountryName { get; set; }
        public string PaymentTerm { get; set; }
        public string PaymentMethod { get; set; }
        public string CurrencyCode { get; set; }
        public string Blocked { get; set; }
        public string TelephoneNumber { get; set; }
        public string TaxCode { get; set; }
    }
}