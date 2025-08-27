using A1AR.SVC.Worker.Lib.Common;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Data;
using System.Xml;
using System.Threading.Tasks;
using Dapper;
using System.Collections.Generic;
using System.Linq;
using Fjord1.Int.API.Models.DB;
using Fjord1.Int.API.Utilities;

namespace Fjord1.Int.API.Workers
{
    public class MoveXML : Worker, IWorkerSettings<WorkerSettings>
    {
        private readonly ILogger<Worker> _workerLogger;
        private readonly WorkerSettings _settings;
        public Type SettingsType => typeof(WorkerSettings);
        //public Type ParametersType => typeof(ParameterType);

        public MoveXML(ILogger<Worker> workerLogger, WorkerSettings WorkerSettings)
        {
            _workerLogger = workerLogger;
            _settings = WorkerSettings;
        }

        public List<string> filetobestyled;
        private string taxPercent;

        public override async Task<JobResult> Execute()
        {
            var files = Directory.GetFiles(_settings.IncomingPath, "*.xml", SearchOption.TopDirectoryOnly);
            XmlDocument doc = new XmlDocument();
            var nsmgr = new XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace("cac", "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2");
            nsmgr.AddNamespace("cbc", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2");

            foreach (string file in files)
            {
                try
                {
                    XmlContent xmlc = new XmlContent();

                    try
                    {
                        doc.Load(file);
                    }
                    catch (Exception ex)
                    {
                        _workerLogger.LogError("Error opening XML file - moved to Error catalogue: " + ex.Message);
                        File.Move(file, $"{_settings.ErrorPath}{Path.GetFileName(file)}");
                        continue;
                    }

                    var docNo = 0;
                    _workerLogger.LogInformation(" ");
                    _workerLogger.LogInformation("Processing file " + file);

                    using IDbConnection dbConnectionAmos = _settings.AmosDbConnection.CreateConnection();
                    dbConnectionAmos.Open();
                    var SQLStringDelPDF = @"Delete from A1ATE_FormPDF";
                    dbConnectionAmos.Execute(SQLStringDelPDF);
                    if (doc.DocumentElement.Name.Contains("CreditNote")) { xmlc.VoucherMulti = -1; }

                    //if (doc.DocumentElement.Name.Contains("CreditNote")) if (doc.DocumentElement.Name == "CreditNote") 

                    foreach (XmlNode node in doc.DocumentElement)
                    {
                        if (node.Name == "cbc:ID") { xmlc.InvoiceId = node.InnerText; }
                        if (node.Name == "cbc:IssueDate")
                        {
                            xmlc.VendorDate = node.InnerText;
                            DateTime VendorDateTime = DateTime.Parse(xmlc.VendorDate);
                            if (VendorDateTime < Convert.ToDateTime("2000-01-01"))
                            {
                                VendorDateTime = DateTime.Today;
                                xmlc.VendorDate = Convert.ToString(VendorDateTime);
                            }
                        }
                        if (node.Name == "cbc:DueDate") { xmlc.DueDate = node.InnerText; }
                        if (node.Name == "cbc:DocumentCurrencyCode") { xmlc.CurrencyCode = node.InnerText; }
                        foreach (XmlNode child in node.ChildNodes)
                        {

                            if ((node.Name == "cac:OrderReference" || node.Name == "xx1:OrderReference") && (child.Name == "cbc:ID" || child.Name == "xx2:ID")) 
                            { 
                                xmlc.VendorRef = string.Concat(child.InnerText.Where(c => !char.IsWhiteSpace(c))); 
                            }

                            if (xmlc.VendorRef.IsNumeric())
                            {
                                using IDbConnection dbConnectionUBW = _settings.UBWDbConnection.CreateConnection();
                                dbConnectionUBW.Open();
                                var SQLStringSelectOrder = @"Select ACCOUNTABLE, ORDER_ID, Estimated_amount
                                                            From A1AR_APOREADY
                                                            Where status='O' and Order_id = @VendorRef";
                                foreach (var value in dbConnectionUBW.Query<A1ar_apoready>(SQLStringSelectOrder, new { xmlc.VendorRef }))
                                {
                                    if (!string.IsNullOrEmpty(value.Accountable)) // i.e. order found in Apoready
                                    {
                                        xmlc.Accountable = value.Accountable;
                                        xmlc.Order_id = value.Order_id;
                                        xmlc.Estimated_amount = value.Estimated_amount;
                                        if (node.Name == "cac:LegalMonetaryTotal" && child.Name == "cbc:TaxExclusiveAmount") { xmlc.InvoiceAmt = Convert.ToDouble(child.InnerText) * xmlc.VoucherMulti; }
                                    }
                                    // PDF Attachment
                                    if (node.Name == "cac:AdditionalDocumentReference" && child.Name == "cac:Attachment")
                                    {
                                        XmlNodeList addocrefr = doc.SelectNodes("descendant::cac:AdditionalDocumentReference/cac:Attachment/cbc:EmbeddedDocumentBinaryObject", nsmgr);
                                        docNo++;
                                        foreach (XmlNode pdfnode in addocrefr)
                                        {
                                            if (pdfnode.Name == "cbc:EmbeddedDocumentBinaryObject" && pdfnode.Attributes["mimeCode"].Value == "application/pdf")
                                            {
                                                xmlc.PDFattachment = pdfnode.InnerText;
                                                if (!string.IsNullOrWhiteSpace(xmlc.PDFattachment))
                                                {
                                                    try
                                                    {
                                                        byte[] PDFBinaryData = Convert.FromBase64String(xmlc.PDFattachment);
                                                        var fileName = pdfnode.Attributes["filename"].Value;
                                                        fileName = fileName.Split("\\").Last();
                                                        var fileLocation = _settings.InvoicePath + Path.GetFileNameWithoutExtension(file) + "_" + fileName + docNo.ToString() + ".PDF";
                                                        FileStream PDFFileStream;
                                                        PDFFileStream = new FileStream(_settings.InvoicePath + Path.GetFileNameWithoutExtension(file) + "_" + fileName + docNo.ToString() + ".PDF", FileMode.Create, FileAccess.Write);
                                                        PDFFileStream.Write(PDFBinaryData, 0, PDFBinaryData.Length);
                                                        PDFFileStream.Close();
                                                        var SQLStringAddPDF = @"Insert Into A1ATE_FormPDF (VendorRef, FileLocation, Filename)
                                                                                Values (@VendorRef, @fileLocation, @filename) ";
                                                        dbConnectionAmos.Execute(SQLStringAddPDF, new { xmlc.VendorRef, fileLocation, fileName });
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        _workerLogger.LogInformation(ex.Message);
                                                        return JobResult.Success("Failed to convert PDF attachment: " + ex.Message);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    // eof PDF
                                    if (node.Name == "cac:AccountingSupplierParty" && child.Name == "cac:Party")
                                    {
                                        foreach (XmlNode subchild in child.ChildNodes)
                                        {
                                            if (subchild.Name == "cbc:EndpointID" || subchild.Name == "cac:PartyIdentification") xmlc.Vat_reg_no = subchild.InnerText;
                                        }
                                    }
                                    if (node.Name == "cac:PaymentMeans" && child.Name == "cac:PayeeFinancialAccount" && child.InnerText.Length == 11) { xmlc.BankAccount = child.InnerText; }
                                    if (node.Name == "cac:PaymentMeans" && child.Name == "cbc:PaymentDueDate")
                                    {
                                        xmlc.DueDate = child.InnerText;
                                        DateTime MyDateTime = DateTime.Parse(xmlc.DueDate);
                                        if (MyDateTime < Convert.ToDateTime("2000-01-01"))
                                        {
                                            MyDateTime = DateTime.Today;
                                            xmlc.DueDate = MyDateTime.ToString("yyyy-MM-dd");
                                        }
                                    }
                                    if (node.Name == "cac:LegalMonetaryTotal" && child.Name == "cbc:TaxExclusiveAmount") 
                                    {
                                        xmlc.Amount = Math.Abs(Convert.ToDouble(child.InnerText)) * xmlc.VoucherMulti;
                                    }
                                    if (node.Name == "cac:LegalMonetaryTotal" && child.Name == "cbc:TaxInclusiveAmount") 
                                    {
                                        xmlc.AmountV = Math.Abs(Convert.ToDouble(child.InnerText)) * xmlc.VoucherMulti;
                                    }
                                    // Tax percent
                                    XmlNode nodes = doc.SelectSingleNode("//cbc:Percent", nsmgr);
                                    taxPercent = nodes.InnerText;
                                    //XmlNode nodes = doc.SelectSingleNode("//cac:TaxTotal//cac:TaxSubtotal//cac:TaxCategory//cbc:Percent", nsmgr);
                                }
                            }
                        }
                    }

                    //if (xmlc.Accountable == "DATANOVA") ProcessDatanovaInvoice(file, xmlc.VendorRef, xmlc.InvoiceId, xmlc.Amount, xmlc.Vat_reg_no, xmlc.BankAccount);
                    if (xmlc.Accountable == "AMOS") ProcessAmosInvoice(file, xmlc.Order_id, xmlc.Estimated_amount, xmlc.Amount, xmlc.AmountV,
                        xmlc.VendorRef, xmlc.InvoiceId, xmlc.InvoiceAmt, xmlc.CurrencyCode, xmlc.DueDate, xmlc.VendorDate);
                    if (xmlc.Accountable != "AMOS" && xmlc.Accountable != "DATANOVA")
                    {
                        File.Move(file, $"{_settings.RootPath}{Path.GetFileName(file)}", true);
                    }
                }
                catch (Exception ex)
                {
                    return JobResult.Failed("Failed: " + ex.Message);
                }
            }
            return JobResult.Success("OK");
        }

        public JobResult ProcessAmosInvoice(string file, string orderId, double orderValue, double Amount, double AmountV, string VendorRef, string InvoiceId, double InvoiceAmt, string CurrencyCode, string DueDate, string VendorDate)
        {
            _workerLogger.LogInformation("Processing Amos Voucher...");
            using IDbConnection dbConnectionUBW = _settings.UBWDbConnection.CreateConnection();

            var filename = Path.GetFileName(file);
            var orderStatus = 'R';
            if (Amount > 0 && Amount < orderValue) orderStatus = 'P';
            if (Amount < 0) orderStatus = 'K';
            //var SQLStringInsert = @"Insert Into A1AR_APOREADY(accountable, order_id, status, last_update, client, apar_id, responsible, estimated_amount, order_date, invoice_no, invoice_amount, filename, duedate, voucher_date, currency)
            //                        select 'AMOS', order_id, @orderStatus, getdate(), '50', apar_id, responsible, estimated_amount, order_date, @InvoiceId, @Amount, @filename, @DueDate, @VendorDate, @CurrencyCode
            //                        from A1AR_APOREADY
            //                        Where Accountable = 'AMOS' and Order_id = @order_id and Status = 'O'";
         //   var SQLStringInsert = @"Merge atsproject AS dst
									//Using (Values('99910','H4')) AS src (project, client)
         //                           On dst.project = src.project and dst.client = src.client
         //                           When not Matched 
									//Then
         //                               Insert (client, project, description,         resource_id, department, date_from,    date_to,      dim1, status, confirm_type, unit_id, customer_id, last_update) 
         //                               Values ('H4',   '99910', 'Eriks prosjekt 1', '10035',     '200',      '2022-06-01', '2022-12-31',  2,    'N',    'P',  'EX',    '900',       GETDATE());";
            var SQLStringInsert = @"Merge a1ar_apoready T
                                    Using (select top 1 accountable, order_id, filename, apar_id, responsible, estimated_amount, order_date from a1ar_apoready Where Accountable = 'AMOS' and Order_id = @order_id and Status = 'O') as S
                                    on (T.Accountable = 'AMOS' and T.invoice_no = @InvoiceId and T.order_id = @order_id)
                                    When Matched 
                                    Then Update set t.filename = @filename
                                    When not Matched by Target
                                    Then Insert (accountable, order_id, status, last_update, client, apar_id, responsible, estimated_amount, order_date, invoice_no, invoice_amount, filename, duedate, voucher_date, currency)
                                         Values( 'AMOS', S.order_id, @orderStatus, getdate(), '50', S.apar_id, S.responsible, S.estimated_amount, S.order_date, @InvoiceId, @Amount, @filename, @DueDate, @VendorDate, @CurrencyCode);";
            
            dbConnectionUBW.Execute(SQLStringInsert, new { order_id = orderId, InvoiceId, Amount, filename, DueDate, orderStatus, VendorDate, CurrencyCode });
            var OrderID = GetOrderId(VendorRef);
            var VATAmount = AmountV - Amount;
            var taxCode = "0";
            if (Convert.ToDouble(taxPercent) == 15.00) taxCode = "0";
            if (Convert.ToDouble(taxPercent) == 25.00) taxCode = "7";
            if (Convert.ToDouble(taxPercent) == 6.00 || Convert.ToDouble(taxPercent) == 12.00) taxCode = "9";

            // Style XML file:
            // 1. Move to raw
            File.Copy(file, $"{_settings.XmlrawPath}{Path.GetFileName(file)}", true);
            // 2. Read file from raw and insert stylesheet
            filetobestyled = Directory.GetFiles(_settings.XmlrawPath, "*.xml", SearchOption.TopDirectoryOnly).ToList();
            var stylesheet = $@"<?xml-stylesheet type=""text/xsl"" href=""{_settings.Stylesheet}{_settings.StylesheetName }""?>";
            var allLines = File.ReadAllLines(filetobestyled[0]).ToList();
            allLines.Insert(1, stylesheet);
            File.WriteAllLines(filetobestyled[0], allLines.ToArray());
            // 3. Move to XML directory from raw
            File.Move(filetobestyled[0], $"{_settings.XmlPath}{Path.GetFileName(filetobestyled[0])}", true);
            // End styling XML file

            #region logging
            //_workerLogger.LogInformation("VendorDate: " + VendorDate);
            //_workerLogger.LogInformation("invoiceid: " + InvoiceId);
            //_workerLogger.LogInformation("CurrencyCode: " + CurrencyCode);
            //_workerLogger.LogInformation("Amount: " + Amount);
            //_workerLogger.LogInformation("DueDate: " + DueDate);
            //_workerLogger.LogInformation("OrderID: " + OrderID);
            //_workerLogger.LogInformation("VATAmount: " + VATAmount);
            //_workerLogger.LogInformation("taxCode: " + taxCode);
            //_workerLogger.LogInformation("taxPercent: " + taxPercent);
            //_workerLogger.LogInformation("vendorref: " + VendorRef); 
            #endregion

            try
            {
                using IDbConnection dbConnectionAmos = _settings.AmosDbConnection.CreateConnection();
                //_workerLogger.LogInformation(@$"A1ATE_MoveFAKFromAgressoData: '{VendorDate}', '{InvoiceId}', '{CurrencyCode}', {Amount}, '{DueDate}', {OrderID}, {VATAmount},'{taxCode}',{taxPercent}");
                var SQLStringExec2 = $"Exec Amos.A1ATE_MoveFAKFromAgressoData '{VendorDate}', '{InvoiceId}', '{CurrencyCode}', {Amount}, '{DueDate}', {OrderID}, {VATAmount},'{taxCode}',{taxPercent}";
                dbConnectionAmos.Execute(SQLStringExec2);

                var SQLStringExec3 = $"Exec Amos.A1ATE_FAK_XML '{VendorRef}', '{_settings.XmlPath}{Path.GetFileName(filetobestyled[0])}','{InvoiceId}'";
                dbConnectionAmos.Execute(SQLStringExec3);

                var SQLStringSelPDF = @"With cte as (
	                                    Select vendorref, filelocation, filename,  
		                                    ROW_NUMBER() OVER (PARTITION BY filename Order by filename ) row_num
		                                    FROM A1ATE_FormPDF)
                                        Select VendorRef, Filelocation, Filename FROM cte WHERE row_num = 1";
                foreach (var pdf in dbConnectionAmos.Query<A1ATE_FormPDF>(SQLStringSelPDF))
                {
                    if (!string.IsNullOrEmpty(pdf.VendorRef))
                    {
                        _workerLogger.LogInformation("Inserting attachment " + pdf.FileLocation);
                        var SQLStringExec4 = $"Exec Amos.A1ATE_FAK_PDF '{pdf.VendorRef}', '{pdf.FileLocation}','{pdf.FileName}'";
                        dbConnectionAmos.Execute(SQLStringExec4);
                    }
                }
                _workerLogger.LogInformation("Moving Amos invoice to OnHold...");
                File.Move(file, $"{_settings.HoldingPath}{Path.GetFileName(file)}", true);

            }
            catch (Exception ex)
            {
                _workerLogger.LogError(ex.Message);
                return JobResult.Failed("Failed to update Amos: " + ex.Message); ;
            }
            return JobResult.Success("Ok");
        }

        //public void ProcessDatanovaInvoice(string file, string VendorRef, string InvoiceId, double Amount, string Vat_reg_no, string BankAccount)
        //{
        //    using IDbConnection dbConnectionUBW = _settings.UBWDbConnection.CreateConnection();
        //    File.Copy(file, $"{_settings.HoldingPath}{Path.GetFileName(file)}", true);
        //    if (File.Exists($"{_settings.DatanovaPath}{Path.GetFileName(file)}")) File.Delete($"{_settings.DatanovaPath}{Path.GetFileName(file)}");
        //    File.Move(file, $"{_settings.DatanovaPath}{Path.GetFileName(file)}");
        //    var SQLStringUpdateOrder = @"Update A1AR_APOREADY
        //                                Set status = 'R', filename = @filename, last_update=GETDATE(), invoice_no=@InvoiceId, invoice_amount=@Amount, vat_reg_no= @Vat_reg_no, bank_account=@BankAccount
        //                                Where Accountable = 'DATANOVA' and Order_id = @VendorRef and Status = 'O'";
        //    var filename = Path.GetFileName(file);
        //    dbConnectionUBW.Query(SQLStringUpdateOrder, new { VendorRef, filename, InvoiceId, Amount, Vat_reg_no, BankAccount });
        //}

        public string GetOrderId(string VendorRef)
        {
            using IDbConnection dbConnectionAmos = _settings.AmosDbConnection.CreateConnection();
            var SQLStringGetRun = @"SELECT orderid FROM orderform WHERE formno=@VendorRef AND formstatus IN (1, 3)";
            var res = dbConnectionAmos.ExecuteScalar(SQLStringGetRun, new { VendorRef });
            if (res == null) res = "0";
            return res.ToString();
        }
    }
}