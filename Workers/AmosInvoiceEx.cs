using A1AR.SVC.Worker.Lib.Common;
using Microsoft.Extensions.Logging;
using System;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using System.Text.RegularExpressions;
using System.Linq;
using Fjord1.Int.API.Models.DB;
using Fjord1.Int.API.Utilities;

namespace Fjord1.Int.API.Workers
{
    public class AmosInvoiceEx : Worker, IWorkerSettings<WorkerSettings>
	{
        private readonly ILogger<Worker> _workerLogger;
		private readonly WorkerSettings _settings;
        public Type SettingsType => typeof(WorkerSettings);

        public AmosInvoiceEx(ILogger<Worker> workerLogger, WorkerSettings WorkerSettings)
		{
			_workerLogger = workerLogger;
			_settings = WorkerSettings;
        }
        public string Mdhm { get; set; } = DateTime.Now.ToString("MMddHHmm");

        public override async Task<JobResult> Execute()
		{
            try
            {
                using IDbConnection dbConnectionUBW = _settings.UBWDbConnection.CreateConnection();
                dbConnectionUBW.Open();

                var SQLStringDelete1 = @"Delete from a1ar_algbatchinput where 1=1";
                var SQLStringDelete2 = @"delete from algorderid where client = '50'";
                try
                {
                    dbConnectionUBW.Execute(SQLStringDelete1, commandTimeout: 60 * 60);
                    dbConnectionUBW.Execute(SQLStringDelete2, commandTimeout: 60 * 60);
                }
                catch (Exception ex)
                {
                    _workerLogger.LogError(ex.Message);
                    return JobResult.Failed("Error initializing database call");
                }
                using IDbConnection dbConnectionAmos = _settings.AmosDbConnection.CreateConnection();
                dbConnectionAmos.Open();
                _workerLogger.LogInformation("DB: " + dbConnectionAmos.Database);

                var SQLStringSelectPO = @"Select a.OrderID, a.FormNo, a.Title as Title, VendorID, EstimatedTotal, a.CurrencyCode, VendorDeliveryDate, a.DeptID,
                                        u.Name as UName, bg.Code as UserDefText, d.name as Delivery, ac.Code as Project, cc.CostCentreCode as Account, t.TaxCode, 
                                        v.VendorRef as Reference, v.Amount, a.FormStatus, a.PartPayment, a.ActualTotal, a.BudgetAtLineItem,
						                Case
							                When v.Amount < (a.EstimatedTotal - a.EstimatedTotal * .05) or (v.amount < a.EstimatedTotal - 200) then 'Delfaktura' 
							                When v.Amount >= (a.EstimatedTotal - a.EstimatedTotal * .05) or (v.amount >= a.EstimatedTotal - 200) then 'Sluttfaktura' 
						                end as InvoiceType,
	                                    Case
		                                    When v.PaymentApprovedDate is null then GETDATE() else v.PaymentApprovedDate
	                                    end as ApprovedDate,
	                                    Case
		                                    When ua.Name is null then 'Autogodkjent' else ua.name
	                                    end as ApprovedBy,
										Case
											When OrderedDate is null then GETDATE() else OrderedDate
										end as OrderedDate,
                                        v.Amount + v.VATAmount as InvoiceTotal
                                        From Orderform a 
                                        left Join Amosuser u on a.CreatedBy = u.UserID 
                                        left join DeliveryPlace d on a.deliveryplaceid=d.deliveryplaceid
                                        left join AccountCode ac on a.AccountCodeID=ac.AccountCodeID
                                        left join CostCentre cc on a.CostCentreID=cc.CostCentreID
                                        left join VoucherOrderForm vo on a.orderid = vo.OrderID
                                        left join Voucher v on v.VoucherID = vo.VoucherID
                                        left Join Amosuser ua on v.PaymentApprovedBy = ua.UserID 
	                                    left join BudgetCode bc on a.BudgetCodeID=bc.BudgetCodeID
	                                    left join BudgetCodeDef bcd on bc.BudgetCodeDefID = bcd.BudgetCodeDefID
	                                    left join BudgetGroup bg on bcd.BudgetGroupID = bg.BudgetGroupID
                                        left join A1ATE_OrderFormTaxCode t on a.OrderID=t.OrderId and t.vendorref = v.vendorref
                                        join A1AR_OrderInvoice oi on a.FormNo = oi.FormNo and v.VendorRef = oi.InvoiceNo
                                        Where 1 = 1
                                        and a.formno = @formno
                                        and v.vendorref = @invoiceno";

                var ordcount = 0;
                foreach (var poheader in dbConnectionAmos.Query<OrderForm>(SQLStringSelectPO, new { formno = _settings.FormNo, invoiceno = _settings.InvoiceNo }))
                {
                    ordcount++;
                    _workerLogger.LogInformation("Processing order " + poheader.FormNo);
                    poheader.FormNo = poheader.FormNo.Replace("-", "");
                    if (String.IsNullOrWhiteSpace(poheader.Title)) poheader.Title = " ";
                    var Rappgrp = poheader.UserDefText;
                    var Konto = GetAccount(Rappgrp);
                    if (!String.IsNullOrEmpty(poheader.LatestDeliveryDate.ToString())) poheader.LatestDeliveryDate = DateTime.Now;
                    if (poheader.Account == "VH") poheader.Account = "4000";
                    if (!String.IsNullOrEmpty(poheader.Account)) Konto = poheader.Account;
                    var appliedAmount = poheader.InvoiceTotal;

                    var origReference = poheader.Reference;
                    poheader.Reference = Regex.Replace(poheader.Reference, "[^0-9.]", "");
                    if (poheader.EstimatedTotal == 0 && poheader.PartPayment != 0) poheader.EstimatedTotal = poheader.PartPayment;
                    if (poheader.EstimatedTotal == 0 && poheader.ActualTotal != 0) poheader.EstimatedTotal = poheader.ActualTotal;


                    InsertPOH(poheader.FormNo, poheader.Title, poheader.VendorID, poheader.OrderedDate, poheader.CurrencyCode, poheader.LatestDeliveryDate,
                        poheader.Delivery, poheader.UName, Rappgrp, poheader.Account, poheader.Project, poheader.Reference, appliedAmount, poheader.ApprovedBy,
                        poheader.ApprovedDate, poheader.DeptID, poheader.EstimatedTotal, origReference, poheader.Amount);

                    var SQLStringSelectLI = @"Select distinct(OrderLineNo), Name, Price, Quantity , Comment1, Comment2, a.Discount, Price*a.Discount/100*Quantity as DiscAmount, 
                                            b.code as Budsjettkode, d.Code as Rappgrp, TaxCode, TaxPercent, Cancelled
                                            From Orderline a
                                            left join BudgetCode b on a.BudgetCodeID=b.BudgetCodeID
                                            left join BudgetCodeDef c on b.BudgetCodeDefID=c.BudgetCodeDefID
                                            left join BudgetGroup d on d.BudgetGroupID=c.BudgetGroupID
                                            left join VoucherOrderForm vo on a.OrderID = vo.orderid
                                            left join Voucher v on vo.VoucherID = v.VoucherID
                                            left join A1ATE_OrderFormTaxCode t on a.OrderID = t.OrderId and t.vendorref = v.vendorref
                                            Where a.orderid=@OrderId 
                                            and a.status=1
                                            and a.Quantity - ISNULL(a.Cancelled,0) > 0                                            
                                            and t.vendorref = @Reference
                                            and a.Price != 0";

                    foreach (var poline in dbConnectionAmos.Query<OrderLine>(SQLStringSelectLI, new { poheader.OrderID, Reference=origReference }))
                    {
                        //Orderline Transformations
                        _workerLogger.LogInformation("Processing line " + poline.OrderLineNo);
                        if (poline.Name == null) poline.Name = " ";
                        if (string.IsNullOrWhiteSpace(poline.TaxCode)) poline.TaxCode = "0";
                        if (string.IsNullOrWhiteSpace(poheader.Account)) poheader.Account = "4000";
                        if (string.IsNullOrWhiteSpace(poline.Comment1)) poline.Comment1 = "4000";
                        if (string.IsNullOrWhiteSpace(poheader.Account) && poheader.DeptID > 600000000) poheader.Account = "4005";
                        if (string.IsNullOrWhiteSpace(poline.Comment1) && poheader.DeptID > 600000000) poline.Comment1 = "4005";
                        var ksted = InstCode(poheader.DeptID);
                        ksted = ksted.Substring(ksted.Length - 3);
                        if (string.IsNullOrWhiteSpace(poheader.Project))
                        {
                            poheader.Project = "VH40" + ksted;
                            var SQLCheckProject = "Select 1 from AccountCode Where Code = @Project and DeptID = @DeptID";
                            var exists = dbConnectionAmos.Query(SQLCheckProject, new { poheader.Project, poheader.DeptID });
                            if (exists.Count() < 1) poheader.Project = " ";
                        }
                        var Rappgrpl = " ";
                        Rappgrpl = poheader.UserDefText;  // settes lik header rapportgruppe som start
                        _workerLogger.LogInformation("Price1: " + poline.Price);
                        poline.Price = Math.Round(poheader.InvoiceTotal * (poline.Price * (poline.Quantity - poline.Cancelled) / poheader.EstimatedTotal), 2, MidpointRounding.AwayFromZero);
                        _workerLogger.LogInformation("Price2: " + poline.Price);

                        if (poheader.BudgetAtLineItem == 0)
                        {
                            poline.Comment1 = poheader.Account;
                            poline.Comment2 = poheader.Project;
                            poline.Quantity = 1;
                        }

                        if (poheader.BudgetAtLineItem == 1)  // overskrives dersom budsjettering på linjenivå
                        {
                            if (string.IsNullOrWhiteSpace(poline.Comment1)) poline.Comment1 = poheader.Account;
                            if (string.IsNullOrWhiteSpace(poline.Comment2)) poline.Comment2 = poheader.Project;
                            Konto = poline.Comment1;
                            Rappgrpl = poline.Rappgrp;
                            if (String.IsNullOrWhiteSpace(Rappgrpl)) { Rappgrpl = " "; }
                            if ((Konto == "4000" || Konto == "4005") && String.IsNullOrWhiteSpace(Rappgrpl))
                            {
                                _workerLogger.LogInformation("Rapportgruppe satt til 1100 for ordre " + poheader.FormNo);
                                Rappgrpl = "1100";
                            }
                            poline.Quantity = 1;
                        }

                        InsertPOL(poline.Name, poline.Price, poline.Quantity, Rappgrpl, poheader.OrderedDate, poheader.LatestDeliveryDate,
                            poheader.DeptID, Konto, poline.Comment1, poline.Comment2, poline.Discount, poline.DiscAmount, poline.TaxCode, poheader.Reference, poheader.FormNo);
                    } // foreach (var poline...

                    using IDbConnection dbConnectionAgr = _settings.UBWDbConnection.CreateConnection();
                    dbConnectionAgr.Open();


                    var SQLStringInsert = @"Insert into algbatchinput(batch_id, client, article, art_descr, amount_set, period, trans_type, account, dim_1, order_id, tax_code, voucher_type, amount, cur_amount, value_1, dim_6, dim_2,
                                        ext_ord_ref, responsible, deliv_countr, currency, apar_id, accountable, long_info1, order_type, control, terms_id, order_date, responsible2, deliv_date, line_no, dim_value_1, dim_value_6, text1)
                                        select batch_id, client, article, ' ', amount_set, period, trans_type, account, dim_1, order_id, tax_code, voucher_type, amount, cur_amount, value_1, dim_6, ' ', 
                                        ext_ord_ref, responsible, deliv_countr, currency, apar_id, accountable, long_info1, order_type, control, terms_id, order_date, responsible2, deliv_date, line_no, dim_value_1, dim_value_6, text1
                                        from a1ar_algbatchinput
                                        where control = 'A'
                                        Union
                                        Select batch_id, client, article, '<Prosjekt ' + isnull(dim_2, '') + ' Aggregert på konto ' + isnull(account, '') + ' + Rappgrp. ' + isnull(dim_6, '') + '>', amount_set, period, trans_type, account, dim_1, order_id, tax_code, voucher_type, sum(amount * value_1), sum(cur_amount * value_1), 1, dim_6, dim_2,
                                        ext_ord_ref, responsible, deliv_countr, currency, apar_id, accountable, long_info1, order_type, control, terms_id, order_date, responsible2, deliv_date, 0, dim_value_1, dim_value_6, text1
                                        from a1ar_algbatchinput
                                        where control <> 'A' and dim_2 is not null
                                        group by batch_id, client, article, amount_set, period, trans_type, account, dim_1, order_id, tax_code, voucher_type, dim_6, dim_2, article, order_id, ext_ord_ref, responsible, deliv_countr, 
                                        currency, apar_id, accountable, long_info1, order_type, control, terms_id, order_date, responsible2, deliv_date, dim_value_1, dim_value_6, text1
                                        Union
                                        Select batch_id, client, article, art_descr, amount_set, period, trans_type, account, dim_1, order_id, tax_code, voucher_type, amount, cur_amount, value_1, dim_6, ' ',
                                        ext_ord_ref, responsible, deliv_countr, currency, apar_id, accountable, long_info1, order_type, control, terms_id, order_date, responsible2, deliv_date, line_no, dim_value_1, dim_value_6, text1
                                        From a1ar_algbatchinput
                                        Where control <> 'A' and dim_2 is null";

                    var SQLStringUpdate1 = @"Update x
                                        Set x.line_no = x.New_line_no
                                        From (Select line_no, ROW_NUMBER() OVER (ORDER BY article) AS New_line_no
                                                From algbatchinput
                                                Where article = 'AMOS' and control != 'A' and line_no = 0 and order_id = @Reference) x";

                    var SQLStringUpdate2 = @"update algbatchinput
                                        set amount = amount * value_1, cur_amount = cur_amount * value_1
                                        where article = 'AMOS' and value_1 > 0 and art_descr <> '%Aggregert%' and order_id=" + poheader.Reference;

                    var SQLStringUpdate3 = @"update algbatchinput
                                        set status='O', pay_method='IP'
                                        where order_id=" + poheader.Reference;

                    var SQLStringDelete = "Delete from a1ar_algbatchinput where 1=1";

                    try
                    {
                        dbConnectionUBW.Execute(SQLStringInsert, commandTimeout: 60 * 60);
                        dbConnectionUBW.Execute(SQLStringUpdate1, new { poheader.Reference }, commandTimeout: 60 * 60);
                        dbConnectionUBW.Execute(SQLStringUpdate2, commandTimeout: 60 * 60);
                        dbConnectionUBW.Execute(SQLStringUpdate3, commandTimeout: 60 * 60);
                        dbConnectionUBW.Execute(SQLStringDelete, commandTimeout: 60 * 60); // DENNE MÅ KJØRES FOR Å UNNGÅ DUPLIKATER !! 
                        var KostSted = InstCode(poheader.DeptID);
                        if (poheader.FormStatus == 1 && poheader.InvoiceType == "Delfaktura")
                        {
                            poheader.Account = "4000";
                            var ksted = InstCode(poheader.DeptID);
                            ksted = ksted.Substring(ksted.Length - 3);
                            poheader.Project = "VH40" + ksted;
                        }
                    }
                    catch (Exception ex)
                    {
                        _workerLogger.LogError(ex.ToString());
                        return JobResult.Failed(ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                _workerLogger.LogError(ex.ToString());
                return JobResult.Failed(ex.Message);
            }
            return JobResult.Success("OK");
        }
        private void InsertPOH(string FormNo, string Title, double Vendor, DateTime OrderDate, string Currency, DateTime DeliveryDate, string DeliveryPlace, string UName, string Rappgrp, string Account,
                string Project, string Reference, double Amount, string ApprovedBy, DateTime ApprovedDate, int DeptID, double Estimate, string origReference, double NetAmount)
        {
            using IDbConnection dbConnectionUBW = _settings.UBWDbConnection.CreateConnection();

            var SQLStringInsert = @"INSERT INTO A1AR_ALGBATCHINPUT (amount, cur_amount, client, batch_id, currency, trans_type, long_info1, control, order_type, deliv_countr, period, amount_set, terms_id, 
                                    apar_id, accountable,  responsible, responsible2, order_id,  deliv_date,  deliv_addr, order_date, ext_ord_ref, voucher_type, dim_6, dim_2, dim_3, dim_value_6, text1) 
                                    VALUES (@amount, @amount, @client,@batch_id, @currency, @trans_type, @long_name, @control, @order_type, @deliv_countr, @period, 1, @terms_id,  
                                    @apar_id , @accountable, @responsible, @responsible2, @order_id, @deliv_date, @deliv_addr, @order_date, @ext_ord_ref, @voucher_type, @dim_6, @dim_2, 
                                    @dim_3, @dim_value_6, @text1)";
            try
            {
                var KostSted = InstCode(DeptID);
                dbConnectionUBW.Execute(SQLStringInsert, new
                {
                    amount = Amount,
                    client = 50,
                    batch_id = "AMOS" + Mdhm,
                    currency = Currency,
                    trans_type = 41,
                    control = 'A',
                    order_type = "IK",
                    deliv_countr = "NO",
                    terms_id = 10,
                    apar_id = Vendor,
                    voucher_type = "IF",
                    period = OrderDate.ToString("yyyyMM"),
                    accountable = UName.SafeSubstring(0, 24),
                    responsible = "AMOS",
                    responsible2 = "AMOS",
                    order_id = Reference, // bort !
                    deliv_date = DeliveryDate,
                    deliv_addr = DeliveryPlace,
                    order_date = OrderDate,
                    long_name = Title + " ",
                    ext_ord_ref = FormNo,
                    dim_2 = Project,
                    dim_3 = Account,
                    dim_6 = Rappgrp,
                    dim_value_6 = Rappgrp,
                    text1 = origReference
                }, commandTimeout: 60 * 60);

                var SQLStringUpdate = @"Update A1AR_APOREADY
                                        Set Status='A', last_update=GETDATE(), ApprovedBy = @ApprovedBy, ApprovedDate = @ApprovedDate
                                        Where Accountable = 'AMOS' 
                                        and invoice_no = @origReference
                                        and Order_id = @FormNo";
                dbConnectionUBW.Execute(SQLStringUpdate, new { FormNo, origReference, ApprovedBy, ApprovedDate }, commandTimeout: 60 * 60);
                if (IsFinalInvoice(Currency, Estimate, NetAmount, OrderDate) == true)
                {
                    var SQLStringUpdateFinal = @"Update A1AR_APOREADY
                                                Set Status='A', last_update=GETDATE(), ApprovedBy = @ApprovedBy, ApprovedDate = @ApprovedDate
                                                Where Accountable = 'AMOS' 
                                                and Status='O'
                                                and Order_id = @FormNo";
                    dbConnectionUBW.Execute(SQLStringUpdateFinal, new { FormNo, ApprovedBy, ApprovedDate }, commandTimeout: 60 * 60);
                    using IDbConnection dbConnectionAmos = _settings.AmosDbConnection.CreateConnection();
                    dbConnectionAmos.Open();
                    var SQLStringUpdateFinalTotal = @"Update v
                                                    Set FinalInvoice = 1
                                                    From orderform o
                                                    Inner join VoucherOrderForm vo on o.OrderID=vo.OrderID
                                                    Inner join voucher v on v.VoucherID=vo.VoucherID
                                                    Where o.formno = @FormNo";
                    dbConnectionAmos.Execute(SQLStringUpdateFinalTotal, new { FormNo }, commandTimeout: 60 * 60);
                }
            }
            catch (Exception ex)
            {
                _workerLogger.LogError(ex.Message);
                JobResult.Failed("InsertPOH failed");
            }
        }

        private void InsertPOL(string LineName, double Price, double Quantity, string Rappgrp, DateTime OrderDate, DateTime DeliveryDate, int DeptID,
            string konto, string LAccount, string LProject, double DiscPercent, double Discount, string TaxCode, string Reference, string FormNo)
        {
            using IDbConnection dbConnectionUBW = _settings.UBWDbConnection.CreateConnection();
            dbConnectionUBW.Open();
            var SQLStringInsert = @"INSERT INTO A1AR_ALGBATCHINPUT 
                                    (client,  batch_id,  trans_type,  account,  voucher_type,  amount_set,  article,  dim_1,  dim_6, dim_2, dim_3,
                                    art_descr,  value_1,  amount,  cur_amount, tax_code, order_id, period, deliv_date, order_date, disc_percent, discount, ext_ord_ref) 
                                    VALUES
                                    (@client, @batch_id, @trans_type, @account, @voucher_type, @amount_set, @article, @dim_1, @dim_6, @dim_2, @dim_3,
                                    @art_descr, @value_1, @amount, @cur_amount, @tax_code, @order_id, @period, @deliv_date, @order_date, @disc_percent, @discount, @ext_ord_ref)";
            var KostSted = InstCode(DeptID);
            try
            {
                dbConnectionUBW.Execute(SQLStringInsert, new
                {
                    client = 50,
                    batch_id = "AMOS" + Mdhm,
                    trans_type = 41,
                    voucher_type = "IF",
                    account = konto,
                    amount_set = 1,
                    article = "AMOS",
                    dim_1 = KostSted,
                    dim_2 = LProject,
                    dim_3 = LAccount,
                    dim_6 = Rappgrp,
                    art_descr = LineName,
                    value_1 = Quantity,
                    amount = Price * (1-(DiscPercent/100)),
                    cur_amount = Price * (1 - (DiscPercent / 100)),
                    tax_code = TaxCode,
                    order_id = Reference,
                    period = OrderDate.ToString("yyyyMM"),
                    deliv_date = DeliveryDate,
                    order_date = OrderDate,
                    disc_percent = DiscPercent,
                    discount = Discount,
                    ext_ord_ref = FormNo,
                }, commandTimeout: 60 * 60);
            }
            catch (Exception ex)
            {
                _workerLogger.LogError(ex.Message);
                JobResult.Failed("InsertPOL failed");
            }
        }
        public string GetOrderID(string order)
        {
            using IDbConnection dbConnectionUBW = _settings.UBWDbConnection.CreateConnection();
            dbConnectionUBW.Open();
            var SQLStringGetAvg = @"SELECT ORDER_ID FROM APOHEADER WHERE EXT_ORD_REF = @kode";
            var res = dbConnectionUBW.ExecuteScalar(SQLStringGetAvg, new { kode = order });
            if (res == null) res = "0";
            return res.ToString();
        }
        public string InstCode(int deptid)
        {
            using IDbConnection dbConnectionAmos = _settings.AmosDbConnection.CreateConnection();
            dbConnectionAmos.Open();
            var SQLStringGetInst = @"Select top 1 i.InstCode from Orderline a 
                                    Inner join Department d on a.Deptid=d.Deptid 
                                    Inner join installation i on d.InstID=i.InstID 
                                    Where a.Deptid=@deptid";
            var res = dbConnectionAmos.ExecuteScalar(SQLStringGetInst, new { deptid }).ToString();
            if (res.Length == 1) res = "00" + res;
            if (res.Length == 2) res = "0" + res;
            if (res == "600") // Spesiell håndtering for land-anlegg på 600-serien
            {
                SQLStringGetInst = @"Select comment1 from department where deptid = @deptid";
                res = dbConnectionAmos.ExecuteScalar(SQLStringGetInst, new { deptid }).ToString();
            }
            var InstCode = "000";
            if (Int32.Parse(res) < 699) InstCode = "700" + res;
            if (Int32.Parse(res) < 399) InstCode = "123" + res;
            if (Int32.Parse(res) < 126) InstCode = "113" + res;
            return InstCode;
        }

        public string GetAccount(string rg)
        {
            using IDbConnection dbConnectionUBW = _settings.UBWDbConnection.CreateConnection();
            dbConnectionUBW.Open();
            if (string.IsNullOrEmpty(rg)) return "4000";
            var SQLStringGetAccount = @"Select konto from A1AR_KTORPGRP where Rapportgruppe=@rg ";
            var res = dbConnectionUBW.ExecuteScalar(SQLStringGetAccount, new { rg });
            if (res == null) res = "4000";
            return res.ToString();
        }
        public bool IsFinalInvoice(string currency, double estimate, double amount, DateTime orderDate)
        {
            using IDbConnection dbConnectionUBW = _settings.UBWDbConnection.CreateConnection();
            dbConnectionUBW.Open();
            var tolerance = false;
            var SQLStringGetCur = @"select exch_rate from acrexchrates
                                    where currency = @currency and
                                    date_from = (select max(date_from) from acrexchrates where currency = @currency)";
            var res = dbConnectionUBW.ExecuteScalar(SQLStringGetCur, new { currency, orderDate });
            if (res == null) res = "1";
            var balance = (estimate * Convert.ToDouble(res) - (amount * Convert.ToDouble(res))); //100
            if (estimate * Convert.ToDouble(res) * .05 > balance && balance <= 200) tolerance = true;
            return tolerance;
        }
    }
}