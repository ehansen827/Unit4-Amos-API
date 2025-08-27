using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using A1AR.SVC.Worker.Lib.Common;
using Dapper;
using Fjord1.Int.API.Models.DB;
using Fjord1.Int.API.Services;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Fjord1.Int.API.Workers
{
    public class RsSupplier : Worker<WorkerParameters, WorkerSettings>
    {
        private readonly WorkerSettings _settings;
        private readonly ILogger<Task> _workerLogger;
        private readonly IGetHttpClient _getHttpClient;

        public RsSupplier(ILogger<Task> _workerLogger, WorkerSettings _settings, IGetHttpClient _getHttpClient)
        {
            this._settings = _settings;
            this._workerLogger = _workerLogger;
            this._getHttpClient = _getHttpClient;
        }
        public override async Task<JobResult> Execute(WorkerParameters parameters)
        {
            try
            {
                using (var ubwClient = _getHttpClient.CreateUBW(_settings))
                {
                    //var LastSuccessFulRun = LastSucessfulRun(this.JobInstance.Id).ToString("yyyy-MM-dd HH:mm");
                    //if (!string.IsNullOrEmpty(_settings.LastUpdated)) LastSuccessFulRun = _settings.LastUpdated;
                    string[] rsClient = _settings.RsClient;

                    for (int i = 0; i < rsClient.Length; i++)
                    {
                        HttpResponseMessage clientresponse = await ubwClient.GetAsync($"{_settings.ApiClient}'{rsClient[i]}'");
                        HttpContent clientcontent = clientresponse.Content;
                        var Json1 = await clientcontent.ReadAsStringAsync();
                        var clients = JsonConvert.DeserializeObject<Clients[]>(Json1);

                        HttpResponseMessage dataresponse = await ubwClient.GetAsync($"{_settings.ApiSupplier}'{rsClient[i]}'");
                        HttpContent content = dataresponse.Content;
                        var Json2 = await content.ReadAsStringAsync();
                        var suppliers = JsonConvert.DeserializeObject<List<Supplier>>(Json2);

                        if (suppliers != null && clients != null && clients.Length > 0 && !string.IsNullOrEmpty(clients[0].ClientName))
                        {

                            DirectoryInfo directory = Directory.CreateDirectory(_settings.RsPath + clients[0].ClientName + "\\");
                            string filename = _settings.RsFilename;
                            XmlTextWriter xmlWriter = new XmlTextWriter(directory + filename, Encoding.UTF8);
                            xmlWriter.Formatting = System.Xml.Formatting.Indented;
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
                                xmlWriter.WriteElementString("PaymentTerm", supplier.PaymentTer);
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