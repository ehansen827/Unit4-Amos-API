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
    public class RsF1Bestiller : Worker<WorkerParameters, WorkerSettings>
    {
        private readonly WorkerSettings _settings;
        private readonly ILogger<Task> _workerLogger;
        private readonly IGetHttpClient _getHttpClient;

        public RsF1Bestiller(ILogger<Task> _workerLogger, WorkerSettings _settings, IGetHttpClient _getHttpClient)
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

                        HttpResponseMessage dataresponse = await ubwClient.GetAsync($"{_settings.ApiBestiller}'{rsClient[i]}'");
                        HttpContent content = dataresponse.Content;
                        var Json2 = await content.ReadAsStringAsync();
                        var bestillere = JsonConvert.DeserializeObject<List<Bestiller>>(Json2);

                        if (bestillere != null && clients != null && clients.Length > 0 && !string.IsNullOrEmpty(clients[0].ClientName))
                        {
                            DirectoryInfo directory = Directory.CreateDirectory(_settings.RsPath + clients[0].ClientName + "\\"); 
                            string filename = _settings.RsFilename;
                            XmlTextWriter xmlWriter = new XmlTextWriter(directory + filename, Encoding.UTF8);
                            xmlWriter.Formatting = System.Xml.Formatting.Indented;
                            xmlWriter.WriteStartDocument();
                            xmlWriter.WriteComment("XML document generated with UBW data");
                            xmlWriter.WriteStartElement("MasterDataObjects");

                            foreach (var bestiller in bestillere)
                            {
                                xmlWriter.WriteStartElement("MasterDataObject");
                                xmlWriter.WriteElementString("Value", bestiller.Value);
                                xmlWriter.WriteElementString("Name", bestiller.Name);
                                xmlWriter.WriteElementString("Description", bestiller.Description);
                                xmlWriter.WriteElementString("Active", bestiller.Active);
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
        public class Clients
        {
            public string Client { get; set; }
            public string ClientName { get; set; }
        }

        public class Bestiller
        {
            public string Active { get; set; }
            public string Client { get; set; }
            public string Description { get; set; }
            public string Name { get; set; }
            public string Value { get; set; }
        }
    }
}