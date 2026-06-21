using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
//using System.Security.Cryptography.X509Certificates;
using System.Text;
//using System.Text.RegularExpressions;
using System.Threading.Tasks;
using A1AR.SVC.Worker.Lib.Attributes;
using A1AR.SVC.Worker.Lib.Common;
using Dapper;
using Fjord1.Int.API.Models.DB;
//using Fjord1.Int.API.Utilities;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Fjord1.Int.API.Workers
{
    public class PoAmos : Worker, IWorkerSettings<WorkerSettings>
    {
        private readonly ILogger<Worker> _workerLogger;
        private readonly WorkerSettings _settings;
        public Type SettingsType => typeof(WorkerSettings);

        public PoAmos(ILogger<Worker> workerLogger, WorkerSettings WorkerSettings)
        {
            _workerLogger = workerLogger;
            _settings = WorkerSettings;
        }

        public class RestDatasourceSettings 
        { 
            [WorkerDatasource("https://jsonplaceholder.typicode.com/")] 
            public IWSConnectionFactory ApiConnectionUrl { get; set; } 
        }
        //public string Mdhm { get; set; } = DateTime.Now.ToString("MMddHHmm");

        public override async Task<JobResult> Execute()
        {
            //using IDbConnection dbConnectionUBW = _settings.UBWDbConnection.CreateConnection();
            //dbConnectionUBW.Open();
            //var SQLStringDelete = @"delete from algorderid where client = '50'";
            //try
            //{
            //    dbConnectionUBW.Execute(SQLStringDelete, commandTimeout: 60 * 60);
            //}
            //catch (Exception ex)
            //{
            //    _workerLogger.LogError(ex.Message);
            //    return JobResult.Failed("Error initializing database call");
            //}

            using IDbConnection dbConnectionAmos = _settings.AmosDbConnection.CreateConnection();
            dbConnectionAmos.Open();
            var SQLSelectPO = @"
                SELECT 
	                orf.OrderID, 
	                orf.FormNo, 
	                orf.Title, 
	                VendorID, 
	                EstimatedTotal, 
	                orf.CurrencyCode, 
	                VendorDeliveryDate, 
	                orf.DeptID,
	                u.Name AS UName, 
	                bg.Code AS UserDefText, 
	                d.name AS Delivery, 
	                ac.Code AS Project, 
	                cc.CostCentreCode AS Account, 
	                t.TaxCode, 
	                v.VendorRef AS Reference, 
                    v.FinalInvoice,
	                v.Amount, 
	                orf.PartPayment, 
	                orf.ActualTotal, 
	                orf.BudgetAtLineItem,
	                CASE
		                WHEN v.Amount < (orf.EstimatedTotal - orf.EstimatedTotal * .05) OR (v.amount < orf.EstimatedTotal - 200) THEN 'Delfaktura' 
		                WHEN v.Amount >= (orf.EstimatedTotal - orf.EstimatedTotal * .05) OR (v.amount >= orf.EstimatedTotal - 200) THEN 'Sluttfaktura' 
	                END AS InvoiceType,
	                Case
		                WHEN v.PaymentApprovedDate IS NULL THEN GETDATE() 
		                ELSE v.PaymentApprovedDate
	                END AS ApprovedDate,
	                Case
		                WHEN ua.Name IS NULL THEN 'Autogodkjent' 
		                ELSE ua.name
	                END AS ApprovedBy,
	                CASE
		                WHEN OrderedDate IS NULL THEN GETDATE() 
		                ELSE OrderedDate
	                END AS OrderedDate,
	                v.Amount + v.VATAmount AS InvoiceTotal,
                    orl.OrderLineNo,
	                orl.Name, 
	                orl.Price, 
	                orl.Quantity, 
	                orl.Comment1, 
	                orl.Comment2, 
	                orl.Discount, 
	                Price*orl.Discount/100*orl.Quantity AS DiscAmount, 
	                bc.code AS Budsjettkode,
	                d.Code AS Rappgrp, 
	                t.TaxCode, 
	                TaxPercent, 
	                orl.Cancelled

                FROM Orderform orf 
                JOIN orderline orl				ON orl.OrderID = orf.OrderID
                LEFT JOIN Amosuser u			ON u.UserID = orf.CreatedBy
                LEFT JOIN DeliveryPlace d		ON d.deliveryplaceid = orf.deliveryplaceid
                LEFT JOIN AccountCode ac		ON ac.AccountCodeID = orf.AccountCodeID
                LEFT JOIN CostCentre cc			ON cc.CostCentreID = orf.CostCentreID
                LEFT JOIN VoucherOrderForm vo	ON vo.OrderID = orf.orderid
                LEFT JOIN Voucher v				ON v.VoucherID = vo.VoucherID
                LEFT JOIN Amosuser ua			ON ua.UserID  = v.PaymentApprovedBy
                LEFT JOIN BudgetCode bc			ON bc.BudgetCodeID = orf.BudgetCodeID
                LEFT JOIN BudgetCodeDef bcd		ON bcd.BudgetCodeDefID = bc.BudgetCodeDefID
                LEFT JOIN BudgetGroup bg		ON bg.BudgetGroupID = bcd.BudgetGroupID
                LEFT JOIN A1ATE_OrderFormTaxCode t ON orf.OrderID = t.OrderId AND t.vendorref = v.vendorref

                WHERE 1 = 1
                --AND v.VendorRef NOT IN (SELECT InvoiceNo FROM A1AR_OrderInvoice WHERE FormNo = orf.FormNo AND InvoiceNo = v.VendorRef)
                AND v.VendorRef IS NOT NULL
                AND vo.amount!=0
                AND orf.EstimatedTotal != 0
                AND (ua.Name != 'Agresso Fjord1 ERP System' OR ua.Name IS NULL)
                AND (
		                (formtype = 1
		                AND formstatus = 3
		                )
	                OR (formtype = 1
		                AND formstatus IN (1,3)
		                AND v.PaymentApprovedBy IS NOT NULL
		                AND v.PaymentApprovedDate IS NOT NULL
		                )
	                )
                AND orl.status=1
                AND orl.Quantity - ISNULL(orl.Cancelled,0) > 0                                            
                AND orl.Price != 0
                AND (ac.code IS NOT NULL OR cc.CostCentreCode IS NOT NULL)
                AND (orf.LastUpdated > @LastSuccessFulRun OR v.LastUpdated > @LastSuccessFulRun )
                --and formno in ('25068132','25068133')
                --and formno = '25068133'
                and formno = @FormNo
                ORDER BY OrderId, v.VendorRef, OrderLineNo";

            var LastSuccessFulRun = LastSucessfulRun(this.JobInstance.Id).ToString("yyyy-MM-dd HH:mm");
            if (!string.IsNullOrEmpty(_settings.LastUpdated)) LastSuccessFulRun = _settings.LastUpdated;
            _workerLogger.LogInformation($"Last successful run: {LastSuccessFulRun}");

            //var ordcount = 0;

            var lookup = new Dictionary<string, OrderForm>();

            dbConnectionAmos.Query<OrderForm, OrderLine, OrderForm>(
                SQLSelectPO,
                (po, line) => {
                    if (!lookup.TryGetValue(po.FormNo + po.Reference, out var existingPo))
                    {
                        existingPo = po;
                        existingPo.OrderLines = new List<OrderLine>();
                        lookup.Add(po.FormNo + po.Reference, existingPo);
                    }
                    if (line != null)
                        existingPo.OrderLines.Add(line);
                    return existingPo;
                },
                new { LastSuccessFulRun, _settings.FormNo },
                splitOn: "OrderLineNo"
                );

            var reader = lookup.Values;
            var purchaseOrder = new PurchaseOrders();
            var lastUpdated = new LastUpdated();
            var orderBuyerInformation = new OrderBuyerInformation();
            var orderDeliveryInformation = new OrderDeliveryInformation();

            foreach (var po in reader)
            {
                _workerLogger.LogInformation(@$"Processing invoice {po.Reference} for order {po.FormNo} ...");
                purchaseOrder.responsible = "AMOS";
                purchaseOrder.accountable = po.UName?[..Math.Min(25, po.UName.Length)];
                purchaseOrder.orderType = "IK";
                purchaseOrder.contractId = "";
                purchaseOrder.orderDeadline = 0;
                purchaseOrder.orderStatus = "O";
                purchaseOrder.addressType = "";
                purchaseOrder.bflag = 0;
                purchaseOrder.invoiceControl = 2;
                purchaseOrder.orderDate = po.OrderedDate;
                purchaseOrder.period = int.Parse(po.OrderedDate.ToString("yyyyMM"));
                purchaseOrder.templateId = 0;
                purchaseOrder.acknowledgementDate = new DateTime(1900, 1, 1, 0, 0, 0, 0);
                purchaseOrder.acknowledgementStatus = "";
                purchaseOrder.amendno = 0;
                purchaseOrder.companyId = "50";
                purchaseOrder.companyReference = "";
                purchaseOrder.confirmationDate = new DateTime(1900, 1, 1, 0, 0, 0, 0); // !!!!!!!!!!!!
                purchaseOrder.currencyCode = po.CurrencyCode.Substring(0, 3);
                purchaseOrder.debtCollectionCode = "";
                purchaseOrder.discountCode = "";
                purchaseOrder.externaInvoiceReference = po.Reference;
                purchaseOrder.externalOrderId = po.FormNo.ToString();
                purchaseOrder.externalReference = po.FormNo.ToString();
                purchaseOrder.followUp = new DateTime(1900, 1, 1, 0, 0, 0, 0); // !!!!!!!!!!!
                purchaseOrder.footerText = "";
                purchaseOrder.freeText1 = po.Reference;
                purchaseOrder.freeText2 = "";
                purchaseOrder.freeText3 = "";
                purchaseOrder.freeText4 = "";
                purchaseOrder.hasBeenPrinted = false;
                purchaseOrder.hasFixedCurrency = false;
                purchaseOrder.headerDimension1 = "B0";
                purchaseOrder.headerDimension2 = "B1";
                purchaseOrder.headerDimension3 = "";
                purchaseOrder.headerDimension4 = "T1";
                purchaseOrder.headerDimension5 = "";
                purchaseOrder.headerDimension6 = "T3";
                purchaseOrder.headerDimension7 = "";
                purchaseOrder.headerText = po.Reference.ToString();
                purchaseOrder.invoiceRecipient = "";
                purchaseOrder.isBackToBackOrderUsed = false;
                purchaseOrder.isAmountDeliveredControlledOnInvoice = true;
                purchaseOrder.isInvoiceControlDisabled = false;
                purchaseOrder.isOrderedAmountControlledOnInvoice = false;
                purchaseOrder.isQuantityDeliveredControlledOnInvoice = false;
                purchaseOrder.languageCode = po.CurrencyCode.Substring(0, 2);
                lastUpdated.updatedAt = DateTime.Now;
                lastUpdated.updatedBy = "SYSTEM";
                purchaseOrder.lastUpdated = lastUpdated;
                purchaseOrder.leadTimeExcludeNonWorkingDays = false;
                purchaseOrder.ledgerType = "P";
                purchaseOrder.lineDimension1 = "";
                purchaseOrder.lineDimension2 = "";
                purchaseOrder.lineDimension3 = "";
                purchaseOrder.lineDimension4 = "";
                purchaseOrder.lineDimension5 = "";
                purchaseOrder.lineDimension6 = po.UserDefText;
                purchaseOrder.lineDimension7 = "";
                purchaseOrder.mainLedgerType = "P";
                purchaseOrder.orderAccountingTemplate = "";
                purchaseOrder.orderDiscount = 0.0;
                purchaseOrder.orderDiscountPercent = 0.0;
                purchaseOrder.orderExchangeRate = 1.0;
                purchaseOrder.orderLeadTime = 0;
                purchaseOrder.orderNumber = 0;
                purchaseOrder.orderTime = 1406; // ???
                purchaseOrder.overrunPercentageAmountOrdered = 0.0;
                purchaseOrder.overrunPercentageQuantityDelivered = 0.0;
                purchaseOrder.overrunPercentageAmountDelivered = 2.5;
                purchaseOrder.paymentMethod = "IP";
                purchaseOrder.paymentTermsDescription = "";
                purchaseOrder.paymentTermsId = "10";
                purchaseOrder.pcbInvoicing = false;
                purchaseOrder.postTransactionReferece = 0;
                purchaseOrder.requestedBy = "AMOS";
                purchaseOrder.supplierAddressId = "0";
                purchaseOrder.supplierDeliveryAddressId = "";
                purchaseOrder.supplierDeliveryAddressType = "";
                purchaseOrder.supplierId = po.VendorId.ToString();
                purchaseOrder.transactionDate = new DateTime(1900, 1, 1, 0, 0, 0, 0);
                purchaseOrder.transactionNumber = 0;
                purchaseOrder.transactionType = "IF";
                purchaseOrder.treatmentCode = "10";
                purchaseOrder.useGlobalGLAnalysis = false;
                orderBuyerInformation.buyerCompanyID = "50";
                purchaseOrder.orderBuyerInformation = orderBuyerInformation;
                orderDeliveryInformation.deliveryAddressId = 1;
                orderDeliveryInformation.deliveryInformation = "";
                orderDeliveryInformation.deliveryAddressType = "";
                orderDeliveryInformation.deliveryComment = "";
                orderDeliveryInformation.deliveryDate = new DateTime(1900, 1, 1, 0, 0, 0, 0); // !!!!!!!!!!!
                orderDeliveryInformation.deliveryDateType = "";
                orderDeliveryInformation.deliveryDateTypeContent = "D";
                orderDeliveryInformation.deliveryDayTimeLimit = 2359;
                orderDeliveryInformation.deliveryDescription = po.Title;
                orderDeliveryInformation.deliveryMethod = "";
                orderDeliveryInformation.deliveryMethodDescription = "";
                orderDeliveryInformation.deliveryTerms = "DDP";
                orderDeliveryInformation.deliveryTermsDescription = "DDP Leveringsadresse";
                orderDeliveryInformation.manualDeliveryAddress = "";
                orderDeliveryInformation.manualDeliveryCountryCode = "NO";
                orderDeliveryInformation.markingsAddressid = 0;
                orderDeliveryInformation.markingsDeliveryAttention = "";
                orderDeliveryInformation.markingsCountryCode = "";
                orderDeliveryInformation.markingsDeliveryAddress = "";
                orderDeliveryInformation.markingsid = "";
                orderDeliveryInformation.markingsTypeDeliveryAddress = "P";
                orderDeliveryInformation.thirdPartyTypeDeliveryAddress = "";
                purchaseOrder.orderDeliveryInformation = orderDeliveryInformation;

                var orderLineList = new List<OrderLineInformation>();
                var koststed = await GetKoststedAsync(po.DeptID, _settings.BaseUri );

                foreach (var line in po.OrderLines)
                {
                    var lineDimension1 = new LineDimension1();
                    var lineDimension2 = new LineDimension2();
                    var lineDimension3 = new LineDimension3();
                    var lineDimension4 = new LineDimension4();
                    var lineDimension5 = new LineDimension5();
                    var lineDimension6 = new LineDimension6();
                    var lineDimension7 = new LineDimension7();

                    lineDimension1.attributeId = "C1";
                    lineDimension1.dimValue = koststed ?? string.Empty;
                    lineDimension2.attributeId = "B0";
                    lineDimension2.dimValue = po.Project;
                    lineDimension3.attributeId = "";
                    lineDimension3.dimValue = "";
                    lineDimension4.attributeId = "";
                    lineDimension4.dimValue = "";
                    lineDimension5.attributeId = "T2";
                    lineDimension5.dimValue = "";
                    lineDimension6.attributeId = "T3";
                    lineDimension6.dimValue = po.UserDefText;
                    lineDimension7.attributeId = "Z21";
                    lineDimension7.dimValue = "";

                    orderLineList.Add(new OrderLineInformation
                    {
                        warehouse = "",
                        allocationKey = 0,
                        productText = "",
                        lineNumber = line.OrderLineNo,
                        currencyAmount = line.Price * line.Quantity,
                        deliveryDate = new DateTime(1900, 1, 1, 0, 0, 0, 0),
                        account = po.Account,
                        amount = line.Price * line.Quantity,
                        contractId = "",
                        location = "",
                        orderDate = po.OrderedDate,
                        orderDiscount = 0.0,
                        orderTimestamp = 0,
                        taxAmount = 0.0,
                        taxCode = line.TaxCode,
                        taxSystem = "",
                        unitPrice = line.Price,
                        accountingTemplate = "",
                        amountDelivered = 0.0,
                        attId1 = "C1",
                        attId2 = "B0",
                        attId3 = "",
                        attId4 = "",
                        attId5 = "T2",
                        attId6 = "T3",
                        attId7 = "Z21",
                        batchId = "",
                        currencyCode = po.CurrencyCode,
                        deliveredNumber = 0.0,
                        guaranted = 0,
                        initialAmount = line.Price * line.Quantity,
                        initialDeliveryDate = new DateTime(1900, 1, 1, 0, 0, 0, 0),
                        initialQuantity = line.Quantity,
                        invoicedQuantity = line.Quantity,
                        isBackToBackOrderUsed = true,
                        isBonus = false,
                        isLineToBePrinted = true,
                        isProductKit = false,
                        isAmountUse = false,
                        ledgerType = "P",
                        lineDeliveryDate = new DateTime(1900, 1, 1, 0, 0, 0, 0),
                        lineDimension1 = lineDimension1,
                        lineDimension2 = lineDimension2,
                        lineDimension3 = lineDimension3,
                        lineDimension4 = lineDimension4,
                        lineDimension5 = lineDimension5,
                        lineDimension6 = lineDimension6,
                        lineDimension7 = lineDimension7,
                        lineDiscount = 0.0,
                        lineDiscountAmount = 0.0,
                        lineDiscountPercent = 0.0,
                        lineExchangeRate = 1.0,
                        lineHasBeenPrinted = false,
                        linePeriod = int.Parse(po.OrderedDate.ToString("yyyyMM")),
                        lineStatus = "O",
                        lineToBePrinted = false,
                        orderNumber = 0,
                        orderTime = 0,
                        originalQuantity = line.Quantity,
                        pageBreak = "N",
                        percentageSplit = 100.0,
                        postedInvoiceAmount = 0,
                        productDescription = line.Name,
                        productGroup = "AMOS",
                        productId = "AMOS",
                        quantity = line.Quantity,
                        registeredInvoiceAmount = 0,
                        requisition = 0,
                        returnedAmount = 0.0,
                        returnedUnits = 0.0,
                        sellerProduct = "AMOS",
                        sellerProductDescription = line.Name,
                        sequenceNumber = 0,
                        serialNumber = "",
                        supplierId = po.VendorId.ToString(),
                        taxCurrencyAmount = 0.0,
                        taxPercentage = 0.0,
                        toBeDelivered = 0.0,
                        unit = "ST",
                        unitDescription = "STK",
                        updatedBy = "SYSTEM",
                        lastUpdated = lastUpdated,
                        workflowState = "N"

                    });
                }
                purchaseOrder.orderLineInformation = orderLineList;


                // Send POST request
                try
                {
                    var jsonContent = JsonConvert.SerializeObject(purchaseOrder, new JsonSerializerSettings
                    {
                        ContractResolver = new CamelCasePropertyNamesContractResolver(),
                        NullValueHandling = NullValueHandling.Ignore
                    });
                    _workerLogger.LogInformation($"Serialized JSON content: {jsonContent}");

                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                    var httpClient = Unit4PurchaseOrderService(_settings.BaseUri, _settings.UserNameUBW,_settings.PasswordUBW);
                    var url = $"{_settings.BaseUri}/v1/purchase-orders".TrimStart('/');
                    var response = await httpClient.PostAsync(url, content);
                    _workerLogger.LogInformation($"HTTP POST to {url} completed with status code {response.StatusCode}");

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        _workerLogger.LogInformation($"Success! Status Code: {response.StatusCode}");
                        _workerLogger.LogInformation($"Response: {responseContent}");
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        _workerLogger.LogInformation($"Error! Status Code: {response.StatusCode}");
                        _workerLogger.LogInformation($"Error Details: {errorContent}");
                    }
                }
                catch (HttpRequestException ex)
                {
                    _workerLogger.LogInformation($"Request failed: {ex.Message}");
                }
                catch (Exception ex)
                {
                    _workerLogger.LogInformation($"Unexpected error: {ex.Message}");
                }

                var SQLInsertOrdInv = @"INSERT INTO A1AR_OrderInvoice(formno, invoiceno, amount, date) 
                                        SELECT @FormNo, @Reference, @Amount, Getdate()
                                        WHERE NOT EXISTS (SELECT * FROM A1AR_OrderInvoice WHERE FormNo = @FormNo AND invoiceno = @Reference)";
                dbConnectionAmos.Execute(SQLInsertOrdInv, new { po.FormNo, po.Reference, po.Amount });

                var SQLUpdateApor = @"UPDATE A1AR_APOREADY
                                    SET Status='A', last_update=GETDATE(), ApprovedBy = @ApprovedBy, ApprovedDate = @ApprovedDate
                                    WHERE Accountable = 'AMOS' 
                                    AND invoice_no = @Reference
                                    AND Order_id = @FormNo";
                dbConnectionAmos.Execute(SQLUpdateApor, new { po.FormNo, po.Reference, po.ApprovedBy, po.ApprovedDate }, commandTimeout: 60 * 60);
            }
            return JobResult.Success("OK");
        }

        public async Task<string> GetKoststedAsync(int deptid, string apiBaseUrl)
        {
            using IDbConnection dbConnectionAmos = _settings.AmosDbConnection.CreateConnection();
            dbConnectionAmos.Open();
            var SQLStringGetInst = @"SELECT TOP 1 i.Sitecode 
                        FROM Department d 
                        JOIN installation i ON d.InstID=i.InstID 
                        WHERE d.Deptid=@deptid";
            string? res = dbConnectionAmos.ExecuteScalar<string>(SQLStringGetInst, new { deptid });

            var httpClient = Unit4PurchaseOrderService(_settings.BaseUri, _settings.UserNameUBW, _settings.PasswordUBW);
            var url = $"/v1/objects/attribute-values?companyId=50&filter=attributeId%20eq%20'Z8'%20%20and%20attributeValue%20eq%20'{res}'".TrimStart('/');
            var response = await httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var attributeValues = JsonConvert.DeserializeObject<List<AttributeValue>>(responseContent);
                _workerLogger.LogInformation($"Successfully retrieved attribute values for deptid {deptid}. Response: {responseContent}");
                var result = attributeValues?.FirstOrDefault();

                if (result != null && result.owner != null)
                {
                    return result.owner;
                }
                else
                {
                    _workerLogger.LogInformation("AttributeValue or its owner property is null.");
                }
            }
            return string.Empty;
        }

        static HttpClient Unit4PurchaseOrderService(string apiBaseUrl, string username, string password)
        {
            var httpClient = new HttpClient
            {
                BaseAddress = new Uri(apiBaseUrl)
            };

            // Set up Basic Authentication
            var authToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}"));
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authToken);
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            return httpClient;
        }

        public DateTime LastSucessfulRun(Guid taskId)
        {
            using IDbConnection dbConnectionATE = _settings.ATEDbConnection.CreateConnection();
            dbConnectionATE.Open();
            var SQLStringGetRun = @"SELECT max(ti.ExecutionFinish)
                                    FROM ATE.TaskInstances ti
                                    INNER JOIN ATE.TaskInstances td on td.TaskDefinitionId = ti.TaskDefinitionId 
                                    WHERE ti.result = 1 
                                    AND td.Id = @taskId";
            var res = dbConnectionATE.ExecuteScalar<DateTime>(SQLStringGetRun, new { taskId }).ToString("yyyy-MM-dd HH:mm");
            if (res == "0001-01-01 00:00" || Convert.ToDateTime(res) == DateTime.MinValue) res = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            return Convert.ToDateTime(res);
        }
    }
}
