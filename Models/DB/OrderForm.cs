using System;
using System.Collections.Generic;
using System.Text;

namespace Fjord1.Int.NetCore
{
    public partial class OrderForm
    {
        public string InvoiceType { get; set; }
        public int DeptID { get; set; }
        public string Reference { get; set; }
        public string TaxCode { get; set; }
        public double Amount { get; set; }
        public string Rappgrp { get; set; }
        public double InvoiceTotal { get; set; }
        public int InstCode { get; set; }
        public string InstName { get; set; }
        public string Responsible { get; set; }
        public string SuperInt { get; set; }
        public string Comment1 { get; set; }
        public string Delivery { get; set; }
        public string Account { get; set; }
        public string Project { get; set; }
        public string UName { get; set; }
        public double OrderID { get; set; }
        public double LayoutID { get; set; }
        public double CreatedBy { get; set; }
        public double SentBy { get; set; }
        public string ApprovedBy { get; set; }
        public double DeliveryTermID { get; set; }
        public double PaymentTermID { get; set; }
        public double OrderPriorityID { get; set; }
        public double StockClassID { get; set; }
        public double CostCentreID { get; set; }
        public double AccountCodeID { get; set; }
        public double OfOrign { get; set; }
        public string CurrencyCode { get { return _CurrencyCode; } set { _CurrencyCode = value.Trim(); } }
        private string _CurrencyCode;
        public double VendorID { get; set; }
        public double BudgetCodeID { get; set; }
        public double ContractID { get; set; }
        public double DeliveryPlaceID { get; set; }
        public string FormNo { get { return _FormNo; } set { _FormNo = value.Trim(); } }
        private string _FormNo;
        public double FormType { get; set; }
        public double FormStatus { get; set; }
        public double BudgetAtLineItem { get; set; }
        public string Title { get { return _Title; } set { _Title = value.Trim(); } }
        private string _Title;
        public DateTime CreatedDate { get; set; }
        public DateTime ApprovedDate { get; set; }
        public DateTime OrderedDate { get; set; }
        public DateTime ReqnApprovedDate { get; set; }
        public double ReqnApprovedBy { get; set; }
        public DateTime LatestDeliveryDate { get; set; }
        public DateTime ConfirmationDate { get; set; }
        public string ConfirmationRef { get { return _ConfirmationRef; } set { _ConfirmationRef = value.Trim(); } }
        private string _ConfirmationRef;
        public DateTime ReceivedDate { get; set; }
        public DateTime UserDefDate1 { get; set; }
        public DateTime UserDefDate2 { get; set; }
        public DateTime UserDefDate3 { get; set; }
        public string UserDefText { get { return _UserDefText; } set { _UserDefText = value.Trim(); } }
        private string _UserDefText;
        public double UserAddressID { get; set; }
        public DateTime BudgetDate { get; set; }
        public double DeliveryID { get; set; }
        public string Comment2 { get { return _Comment2; } set { _Comment2 = value.Trim(); } }
        private string _Comment2;
        public double EstimatedTotal { get; set; }
        public double Shipping { get; set; }
        public double PartPayment { get; set; }
        public double ActualTotal { get; set; }
        public double VendorAdvisedTotal { get; set; }
        public DateTime VendorDeliveryDate { get; set; }
        public DateTime LatestDeliveryDateForVendor { get; set; }
        public double QueuedForTransfer { get; set; }
        public double QueuedForBusinessToBusiness { get; set; }
        public double WorkFlowStatusID { get; set; }
        public double ExportMarker { get; set; }
        public DateTime LastUpdated { get; set; }
        public double ConvertedFrom { get; set; }
        public double CustomClearanceContractID { get; set; }
        public double QueuedForRemoteWorkFlow { get; set; }
        public double AddressContactID { get; set; }
        public double UserTableID { get; set; }
        public double Criticality { get; set; }
        public string Name { get; set; }
    }
}
