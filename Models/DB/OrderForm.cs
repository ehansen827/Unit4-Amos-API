using System;
using System.Collections.Generic;
using System.Text;

namespace Fjord1.Int.API.Models.DB
{
    public class OrderForm
    {
        public long OrderId { get; set; }
        public int FormNo { get; set; }
        public string Title { get; set; }
        public int VendorId { get; set; }
        public double EstimatedTotal { get; set; }
        public string CurrencyCode { get; set; }
        public DateTime VendorDeliveryDate { get; set; }
        public int DeptID { get; set; }
        public string UName { get; set; }
        public string UserDefText { get; set; }
        public string? Delivery { get; set; }
        public string Project { get; set; }
        public string Account { get; set; }
        public string Reference { get; set; }
        public int FinalInvoice { get; set; }
        public double Amount { get; set; }
        public double PartPayment { get; set; }
        public double ActualTotal { get; set; }
        public int BudgetAtLineItem { get; set; }
        public string InvoiceType { get; set; }
        public DateTime ApprovedDate { get; set; }
        public string ApprovedBy { get; set; }
        public DateTime OrderedDate { get; set; }
        public double InvoiceTotal { get; set; }
        public int WorkFlowStatusID { get; set; }
        public int InstCode { get; set; }
        public string InstName { get; set; }
        public int FormStatus { get; set; }
        public string Responsible { get; set; }
        public string SuperInt { get; set; }
        public DateTime ReceivedDate { get; set; } = DateTime.Now;
        public List<OrderLine> OrderLines { get; set; }
    }
}