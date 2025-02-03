using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fjord1.Int.NetCore
{
    public class XmlContent
    {
        public double Amount { get; set; }
        public double AmountV { get; set; }
        public string BankAccount { get; set; }
        public string VendorRef { get; set; }
        public string InvoiceId { get; set; }
        public double InvoiceAmt { get; set; }
        public string CurrencyCode { get; set; }
        public string RegistrationDate { get; set; }
        public string DueDate { get; set; }
        public string IssueDate { get; set; }
        public string VendorDate { get; set; }
        public string PDFattachment { get; set; }
        public string Vat_reg_no { get; set; }
        public int VoucherMulti { get; set; } = 1;
        public string Accountable { get; set; }
        public string Order_id { get; set; }
        public double Estimated_amount { get; set; }
        public string InvoiceType { get; set; }
        public int TaxPercent { get; set; }
    }

}
