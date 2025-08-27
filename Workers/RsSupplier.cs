using A1AR.SVC.Worker.Lib.Common;
using System;
using Dapper;
using System.Threading.Tasks;
using System.Data;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.IO;
using Fjord1.Int.API.Models.DB;
using System.Xml;
using System.Text;

namespace Fjord1.Int.API.Workers
{
    public class RsSupplier : Worker, IWorkerSettings<WorkerSettings>
    {
        private readonly ILogger<Worker> _workerLogger;
        private readonly WorkerSettings _settings;
        public Type SettingsType => typeof(WorkerSettings);
        public RsSupplier(ILogger<Worker> workerLogger, WorkerSettings WorkerSettings)
        {
            _workerLogger = workerLogger;
            _settings = WorkerSettings;
        }
        public override async Task<JobResult> Execute()
        {
            try
            {
                using IDbConnection dbConnectionUBW = _settings.UBWDbConnection.CreateConnection();
                dbConnectionUBW.Open();
                var LastSuccessFulRun = LastSucessfulRun(this.JobInstance.Id).ToString("yyyy-MM-dd HH:mm");
                if (!string.IsNullOrEmpty(_settings.LastUpdated)) LastSuccessFulRun = _settings.LastUpdated;
                string[] rsClient = _settings.RsClient;

                //var SQLSelectSupplier = @"WITH DistinctSuppliers AS (
                //                        SELECT t.apar_id,
                //                               ROW_NUMBER() OVER (PARTITION BY t.apar_id ORDER BY t.apar_id) AS RowNum
                //                        FROM asuheader t
                //                        INNER JOIN agladdress a1 ON (t.client = a1.Client
                //                                                     AND t.apar_id = a1.dim_value
                //                                                     AND a1.address_type = '1'
                //                                                     AND a1.attribute_id = 'A5')
                //                        WHERE t.client = @Client
                //                          AND t.apar_gr_id IN ('IL', 'KL', 'UL', 'AP')
                //                          AND t.status = 'N'
                //                    )

                //                    SELECT t.apar_id AS SupplierNumber,
                //                           t.apar_name AS Name,
                //                           t.vat_reg_no AS TaxRegistrationNumber,
                //                           t.comp_reg_no AS OrganizationNumber,
                //                           a1.address AS Street,
                //                           a1.zip_code AS PostalCode,
                //                           a1.place AS City,
                //                           a1.country_code AS CountryName,
                //                           t.terms_id AS PaymentTerm,
                //                           t.pay_method AS PaymentMethod,
                //                           t.currency AS CurrencyCode,
                //                           0 AS Blocked,
                //                           a1.telephone_1 AS TelephoneNumber,
                //                           t.tax_code AS TaxCode
                //                    FROM asuheader t
                //                    INNER JOIN agladdress a1 ON (t.client = a1.Client
                //                                                 AND t.apar_id = a1.dim_value
                //                                                 AND a1.address_type = '1'
                //                                                 AND a1.attribute_id = 'A5')
                //                    INNER JOIN DistinctSuppliers ds ON t.apar_id = ds.apar_id
                //                    WHERE t.client = @Client
                //                      AND ds.RowNum = 1
                //                      AND t.apar_gr_id IN ('IL', 'KL', 'UL', 'AP')
                //                      AND t.status = 'N'";

                var SQLSelectSupplier = @"SELECT t.apar_id as SupplierNumber,
                                        t.apar_name aS Name,
                                        t.vat_reg_no as TaxRegistrationMumber,
                                        t.comp_reg_no as OrganizationNumber,
                                        a1.address as Street,
                                        a1.zip_code as PostalCode,
                                        a1.place as City,
                                        a1.country_code as CountryName,
                                        t.terms_id as PaymentTer,
                                        t.pay_method as PaymentMethod,
                                        t.currency as CurrencyCode,
                                        0 AS Blocked,
                                        a1.telephone_1 as TelephoneNumber,
                                        t.tax_code as TaxCode
                                        FROM asuheader t  
                                        INNER JOIN agladdress a1 ON (1=1)
										JOIN acrclient ac on  ac.client = @Client
                                        WHERE t.apar_gr_id IN('IL','IN','KL','UL','AP') 
                                        AND COALESCE(cast(convert(char(8),t.expired_date,112)as datetime) , convert(datetime,'19000101',112)) = convert(datetime,'19000101',112) 
                                        AND t.status='N' 
                                        AND t.client = ac.pay_client 
                                        AND t.apar_id = a1.dim_value 
                                        AND a1.attribute_id= 'A5' 
                                        AND a1.client = t.client
                                        AND a1.address_type = 1
                                        ";

                var SQLSelectClName = @"SELECT client_name FROM acrclient WHERE client = @Client";

                for (int i = 0; i < rsClient.Length; i++)
                {
                    var clname = dbConnectionUBW.QuerySingleOrDefault<string>(SQLSelectClName, new { Client = rsClient[i] });
                    DirectoryInfo di = Directory.CreateDirectory(_settings.RsPath + clname + "\\");
                    var suppliers = dbConnectionUBW.Query<Supplier>(SQLSelectSupplier, new { Client = rsClient[i] });
                    string filename = _settings.RsFilename;
                    XmlTextWriter xmlWriter = new XmlTextWriter(di + filename, Encoding.UTF8);
                    xmlWriter.Formatting = Formatting.Indented;
                    xmlWriter.WriteStartDocument();
                    xmlWriter.WriteComment("XML document generated with UBW data");
                    xmlWriter.WriteStartElement("Suppliers");

                    foreach (var supplier in suppliers)
                    {
                        xmlWriter.WriteStartElement("Supplier");
                        xmlWriter.WriteElementString("SupplierNumber", supplier.SupplierNumber);
                        xmlWriter.WriteElementString("Name", supplier.Name);
                        xmlWriter.WriteElementString("TaxRegistrationNumber", supplier.TaxRegistrationNumber);
                        xmlWriter.WriteElementString("OrganizationNumber", supplier.OrganizationNumber);
                        xmlWriter.WriteElementString("Street", supplier.Street);
                        xmlWriter.WriteElementString("City", supplier.City);
                        xmlWriter.WriteElementString("CountryName", supplier.CountryName);
                        xmlWriter.WriteElementString("PaymentTerm", supplier.PaymentTerm);
                        xmlWriter.WriteElementString("PaymentMethod", supplier.PaymentMethod);
                        xmlWriter.WriteElementString("CurrencyCode", supplier.CurrencyCode);
                        xmlWriter.WriteElementString("Blocked", supplier.Blocked);
                        xmlWriter.WriteElementString("TelephoneNumber", supplier.TelephoneNumber);
                        xmlWriter.WriteElementString("TaxCode", supplier.TaxCode);
                        xmlWriter.WriteEndElement();
                    }
                    xmlWriter.WriteEndElement();
                    xmlWriter.WriteEndDocument();
                    xmlWriter.Flush();
                    xmlWriter.Close();
                }
            }
            catch (Exception ex)
            {
                _workerLogger.LogInformation(ex.Message);
                return JobResult.Failed("Failed");
            }
            return JobResult.Success("OK"); 
        }
        public DateTime LastSucessfulRun(Guid taskId)
        {
            using (IDbConnection dbConnectionATE = _settings.ATEDbConnection.CreateConnection())
            {
                dbConnectionATE.Open();
                var SQLStringGetRun = @"Select max(ti.ExecutionFinish)
                                      From [A1TASKENGINE].[ATE].[TaskInstances] ti
                                      Inner join [A1TASKENGINE].[ATE].[TaskInstances] td on td.TaskDefinitionId = ti.TaskDefinitionId 
                                      Where ti.result = 1 
                                      and td.Id = @taskId";
                var res = dbConnectionATE.ExecuteScalar<DateTime>(SQLStringGetRun, new { taskId }).ToString("yyyy-MM-dd HH:mm");
                return Convert.ToDateTime(res);
            }
        }
    }
}