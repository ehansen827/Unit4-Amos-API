using System;
using System.Collections.Generic;
using System.Text;

namespace Fjord1.Int.API.Models.DB
{
    public class PurchaseOrders
    {
        public string responsible { get; set; }
        public string accountable { get; set; }
        public string orderType { get; set; }
        public string contractId { get; set; }
        public int? orderDeadline { get; set; }
        public string orderStatus { get; set; }
        public string addressType { get; set; }
        public int? bflag { get; set; }
        public int? invoiceControl { get; set; }
        public DateTime? orderDate { get; set; }
        public int? period { get; set; }
        public int? templateId { get; set; }
        public DateTime? acknowledgementDate { get; set; }
        public string acknowledgementStatus { get; set; }
        public int? amendno { get; set; }
        public string companyId { get; set; }
        public string companyReference { get; set; }
        public DateTime? confirmationDate { get; set; }
        public string currencyCode { get; set; }
        public string debtCollectionCode { get; set; }
        public string discountCode { get; set; }
        public string externaInvoiceReference { get; set; }
        public string externalOrderId { get; set; }
        public string externalReference { get; set; }
        public DateTime? followUp { get; set; }
        public string footerText { get; set; }
        public string freeText1 { get; set; }
        public string freeText2 { get; set; }
        public string freeText3 { get; set; }
        public string freeText4 { get; set; }
        public bool? hasBeenPrinted { get; set; }
        public bool? hasFixedCurrency { get; set; }
        public string headerDimension1 { get; set; }
        public string headerDimension2 { get; set; }
        public string headerDimension3 { get; set; }
        public string headerDimension4 { get; set; }
        public string headerDimension5 { get; set; }
        public string headerDimension6 { get; set; }
        public string headerDimension7 { get; set; }
        public string headerText { get; set; }
        public string invoiceRecipient { get; set; }
        public bool? isBackToBackOrderUsed { get; set; }
        public bool? isAmountDeliveredControlledOnInvoice { get; set; }
        public bool? isInvoiceControlDisabled { get; set; }
        public bool? isOrderedAmountControlledOnInvoice { get; set; }
        public bool? isQuantityDeliveredControlledOnInvoice { get; set; }
        public string languageCode { get; set; }
        public LastUpdated lastUpdated { get; set; }
        public bool? leadTimeExcludeNonWorkingDays { get; set; }
        public string ledgerType { get; set; }
        public string lineDimension1 { get; set; }
        public string lineDimension2 { get; set; }
        public string lineDimension3 { get; set; }
        public string lineDimension4 { get; set; }
        public string lineDimension5 { get; set; }
        public string lineDimension6 { get; set; }
        public string lineDimension7 { get; set; }
        public string mainLedgerType { get; set; }
        public string orderAccountingTemplate { get; set; }
        public double? orderDiscount { get; set; }
        public double? orderDiscountPercent { get; set; }
        public double? orderExchangeRate { get; set; }
        public int? orderLeadTime { get; set; }
        public int? orderNumber { get; set; }
        public int? orderTime { get; set; }
        public double? overrunPercentageAmountOrdered { get; set; }
        public double? overrunPercentageQuantityDelivered { get; set; }
        public double? overrunPercentageAmountDelivered { get; set; }
        public string paymentMethod { get; set; }
        public string paymentTermsDescription { get; set; }
        public string paymentTermsId { get; set; }
        public bool? pcbInvoicing { get; set; }
        public int? postTransactionReferece { get; set; }
        public string requestedBy { get; set; }
        public string supplierAddressId { get; set; }
        public string supplierDeliveryAddressId { get; set; }
        public string supplierDeliveryAddressType { get; set; }
        public string supplierId { get; set; }
        public DateTime? transactionDate { get; set; }
        public int? transactionNumber { get; set; }
        public string transactionType { get; set; }
        public string treatmentCode { get; set; }
        public bool? useGlobalGLAnalysis { get; set; }
        public OrderBuyerInformation orderBuyerInformation { get; set; }
        public OrderDeliveryInformation orderDeliveryInformation { get; set; }
        public List<OrderLineInformation> orderLineInformation { get; set; }
    }
    //public class LastUpdated
    //{
    //    public DateTime? updatedAt { get; set; }
    //    public string updatedBy { get; set; }
    //}

    public class LineDimension1
    {
        public string attributeId { get; set; }
        public string dimValue { get; set; }
    }

    public class LineDimension2
    {
        public string attributeId { get; set; }
        public string dimValue { get; set; }
    }

    public class LineDimension3
    {
        public string attributeId { get; set; }
        public string dimValue { get; set; }
    }

    public class LineDimension4
    {
        public string attributeId { get; set; }
        public string dimValue { get; set; }
    }

    public class LineDimension5
    {
        public string attributeId { get; set; }
        public string dimValue { get; set; }
    }

    public class LineDimension6
    {
        public string attributeId { get; set; }
        public string dimValue { get; set; }
    }

    public class LineDimension7
    {
        public string attributeId { get; set; }
        public string dimValue { get; set; }
    }

    public class OrderBuyerInformation
    {
        public string buyerCompanyID { get; set; }
    }

    public class OrderDeliveryInformation
    {
        public int? deliveryAddressId { get; set; }
        public string deliveryInformation { get; set; }
        public string deliveryAddressType { get; set; }
        public string deliveryComment { get; set; }
        public DateTime? deliveryDate { get; set; }
        public string deliveryDateType { get; set; }
        public string deliveryDateTypeContent { get; set; }
        public int? deliveryDayTimeLimit { get; set; }
        public string deliveryDescription { get; set; }
        public string deliveryMethod { get; set; }
        public string deliveryMethodDescription { get; set; }
        public string deliveryTerms { get; set; }
        public string deliveryTermsDescription { get; set; }
        public string manualDeliveryAddress { get; set; }
        public string manualDeliveryCountryCode { get; set; }
        public int? markingsAddressid { get; set; }
        public string markingsDeliveryAttention { get; set; }
        public string markingsCountryCode { get; set; }
        public string markingsDeliveryAddress { get; set; }
        public string markingsid { get; set; }
        public string markingsTypeDeliveryAddress { get; set; }
        public string thirdPartyTypeDeliveryAddress { get; set; }
    }

    public class OrderLineInformation
    {
        public string warehouse { get; set; }
        public int? allocationKey { get; set; }
        public string productText { get; set; }
        public int? lineNumber { get; set; }
        public double? currencyAmount { get; set; }
        public DateTime? deliveryDate { get; set; }
        public string account { get; set; }
        public double? amount { get; set; }
        public string contractId { get; set; }
        public string location { get; set; }
        public DateTime? orderDate { get; set; }
        public double? orderDiscount { get; set; }
        public int? orderTimestamp { get; set; }
        public double? taxAmount { get; set; }
        public string taxCode { get; set; }
        public string taxSystem { get; set; }
        public double? unitPrice { get; set; }
        public string accountingTemplate { get; set; }
        public double? amountDelivered { get; set; }
        public string attId1 { get; set; }
        public string attId2 { get; set; }
        public string attId3 { get; set; }
        public string attId4 { get; set; }
        public string attId5 { get; set; }
        public string attId6 { get; set; }
        public string attId7 { get; set; }
        public string batchId { get; set; }
        public string currencyCode { get; set; }
        public double? deliveredNumber { get; set; }
        public int? guaranted { get; set; }
        public double? initialAmount { get; set; }
        public DateTime? initialDeliveryDate { get; set; }
        public double? initialQuantity { get; set; }
        public double? invoicedQuantity { get; set; }
        public bool? isBackToBackOrderUsed { get; set; }
        public bool? isBonus { get; set; }
        public bool? isLineToBePrinted { get; set; }
        public bool? isProductKit { get; set; }
        public bool? isAmountUse { get; set; }
        public string ledgerType { get; set; }
        public DateTime? lineDeliveryDate { get; set; }
        public LineDimension1 lineDimension1 { get; set; }
        public LineDimension2 lineDimension2 { get; set; }
        public LineDimension3 lineDimension3 { get; set; }
        public LineDimension4 lineDimension4 { get; set; }
        public LineDimension5 lineDimension5 { get; set; }
        public LineDimension6 lineDimension6 { get; set; }
        public LineDimension7 lineDimension7 { get; set; }
        public double? lineDiscount { get; set; }
        public double? lineDiscountAmount { get; set; }
        public double? lineDiscountPercent { get; set; }
        public double? lineExchangeRate { get; set; }
        public bool? lineHasBeenPrinted { get; set; }
        public int? linePeriod { get; set; }
        public string lineStatus { get; set; }
        public bool? lineToBePrinted { get; set; }
        public int? orderNumber { get; set; }
        public int? orderTime { get; set; }
        public double? originalQuantity { get; set; }
        public string pageBreak { get; set; }
        public double? percentageSplit { get; set; }
        public int? postedInvoiceAmount { get; set; }
        public string productDescription { get; set; }
        public string productGroup { get; set; }
        public string productId { get; set; }
        public double? quantity { get; set; }
        public int? registeredInvoiceAmount { get; set; }
        public int? requisition { get; set; }
        public double? returnedAmount { get; set; }
        public double? returnedUnits { get; set; }
        public string sellerProduct { get; set; }
        public string sellerProductDescription { get; set; }
        public int? sequenceNumber { get; set; }
        public string serialNumber { get; set; }
        public string supplierId { get; set; }
        public double? taxCurrencyAmount { get; set; }
        public double? taxPercentage { get; set; }
        public double? toBeDelivered { get; set; }
        public double? toBeInvoiced { get; set; }
        public string unit { get; set; }
        public string unitDescription { get; set; }
        public string updatedBy { get; set; }
        public LastUpdated lastUpdated { get; set; }
        public string workflowState { get; set; }
    }
}