using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fjord1.Int.API.Models.DB
{
    public partial class OrderLine
    {
        public int TaxPercent { get; set; }
        public string TaxCode { get; set; }
        public string Rappgrp { get; set; }
        public string Account { get; set; }
        public string Project { get; set; } = " ";
        public double OrderLineID { get; set; }
        public double ForCompID { get; set; }
        public double UnitID { get; set; }
        public string CurrencyCode { get { return _CurrencyCode; } set { _CurrencyCode = value.Trim(); } }
        private string _CurrencyCode;
        public double PartID { get; set; }
        public double OrderID { get; set; }
        public double LayoutID { get; set; }
        public int OrderLineNo { get; set; }
        public string Name { get { return _Name; } set { _Name = value.Trim(); } }
        private string _Name;
        public string MakerRef { get { return _MakerRef; } set { _MakerRef = value.Trim(); } }
        private string _MakerRef;
        public string ExtraNo { get { return _ExtraNo; } set { _ExtraNo = value.Trim(); } }
        private string _ExtraNo;
        public double Factor { get; set; }
        public double Price { get; set; }
        public double DiscAmount { get; set; }
        public double Quantity { get; set; }
        public double OriginalQuantity { get; set; }
        public double Desired { get; set; }
        public double Discount { get; set; }
        public string Comment1 { get { return _Comment1; } set { _Comment1 = value.Trim(); } }
        private string _Comment1;
        public string Comment2 { get { return _Comment2; } set { _Comment2 = value.Trim(); } }
        private string _Comment2;
        public double Received { get; set; }
        public double Cancelled { get; set; }
        public double Status { get; set; }
        public double LineContent { get; set; }
        public double IncludeOnForm { get; set; }
        public double ToOrderID { get; set; }
        public double FromOrderID { get; set; }
        public DateTime LatestDeliveryDate { get; set; }
        public double Budgeted { get; set; }
        public double BudgetCodeID { get; set; }
        public double WorkOrderID { get; set; }
        public double CostCentreID { get; set; }
        public double ReceiptStatusID { get; set; }
        public int DeptID { get; set; }
        public double ExportMarker { get; set; }
        public DateTime LastUpdated { get; set; }
        public double UserTableID { get; set; }
        public string Reference { get; set; }
    }
}
