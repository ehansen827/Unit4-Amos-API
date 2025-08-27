using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
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
    public class RsPOnummer : Worker<WorkerParameters, WorkerSettings>
    {
        private readonly WorkerSettings _settings;
        private readonly ILogger<Task> _workerLogger;
        private readonly IGetHttpClient _getHttpClient;

        public RsPOnummer(ILogger<Task> _workerLogger, WorkerSettings _settings, IGetHttpClient _getHttpClient)
        {
            this._settings = _settings;
            this._workerLogger = _workerLogger;
            this._getHttpClient = _getHttpClient;
        }
        public override async Task<JobResult> Execute(WorkerParameters parameters)
        {
            try
            {
                using IDbConnection dbConnectionAmos = _settings.AmosDbConnection.CreateConnection();
                dbConnectionAmos.Open();
                //var LastSuccessFulRun = LastSucessfulRun(this.JobInstance.Id).ToString("yyyy-MM-dd HH:mm");
                //if (!string.IsNullOrEmpty(_settings.LastUpdated)) LastSuccessFulRun = _settings.LastUpdated;
                string[] rsClient = _settings.RsClient;

                var SQLSelectPOnr = @"SELECT DISTINCT(order_id) AS Value, 'true' AS Active FROM a1ar_apoready WHERE client = @Client AND status = 'O'";

                for (int i = 0; i < rsClient.Length; i++)
                {
                    var ubwClient = _getHttpClient.CreateUBW(_settings);
                    HttpResponseMessage clientresponse = await ubwClient.GetAsync($"{_settings.ApiClient}'{rsClient[i]}'");
                    HttpContent clientcontent = clientresponse.Content;
                    var Json1 = await clientcontent.ReadAsStringAsync();
                    var clients = JsonConvert.DeserializeObject<Clients[]>(Json1);

                    DirectoryInfo directory = Directory.CreateDirectory(_settings.RsPath + clients[0].ClientName + "\\");
                    var POnummer = dbConnectionAmos.Query<POnummer>(SQLSelectPOnr, new { Client = rsClient[i] });
                    string filename = _settings.RsFilename;
                    XmlTextWriter xmlWriter = new XmlTextWriter(directory + filename, Encoding.UTF8);
                    xmlWriter.Formatting = System.Xml.Formatting.Indented;
                    xmlWriter.WriteStartDocument();
                    xmlWriter.WriteComment("XML document generated with UBW data");
                    xmlWriter.WriteStartElement("MasterDataObjects");

                    foreach (var nummer in POnummer)
                    {
                        xmlWriter.WriteStartElement("MasterDataObject");
                        xmlWriter.WriteElementString("Value", nummer.Value.ToString());
                        xmlWriter.WriteElementString("Name", null);
                        xmlWriter.WriteElementString("Description", null);
                        xmlWriter.WriteElementString("Active", nummer.Active);
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
        public class Clients
        {
            public string Client { get; set; }
            public string ClientName { get; set; }
        }
    }
}