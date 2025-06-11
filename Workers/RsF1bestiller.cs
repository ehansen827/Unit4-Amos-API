using A1AR.SVC.Worker.Lib.Common;
using System;
using Dapper;
using System.Threading.Tasks;
using System.Data;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.IO;
using Fjord1.Int.NetCore.Models.DB;
using System.Xml;
using System.Text;

namespace Fjord1.Int.NetCore
{
    public class RsF1Bestiller : Worker, IWorkerSettings<WorkerSettings>
    {
        private readonly ILogger<Worker> _workerLogger;
        private readonly WorkerSettings _settings;
        public Type SettingsType => typeof(WorkerSettings);
        public RsF1Bestiller(ILogger<Worker> workerLogger, WorkerSettings WorkerSettings)
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

                var SQLSelectBestiller = @"SELECT 'F1BESTILLER' name, dim_value value, description, 'true' active FROM agldimvalue
                                            WHERE client = @Client
                                            AND attribute_id = 'Z21'
                                            AND status = 'N'";
                var SQLSelectClName = @"SELECT client_name FROM acrclient WHERE client = @Client";

                for (int i = 0; i < rsClient.Length; i++)
                {
                    var clname = dbConnectionUBW.QuerySingleOrDefault<string>(SQLSelectClName, new { Client = rsClient[i] });
                    DirectoryInfo di = Directory.CreateDirectory(_settings.RsPath + clname + "\\");
                    var F1bestiller = dbConnectionUBW.Query<F1bestiller>(SQLSelectBestiller, new { Client = rsClient[i] });
                    string filename = _settings.RsFilename;
                    XmlTextWriter xmlWriter = new XmlTextWriter(di + filename, Encoding.UTF8);
                    xmlWriter.Formatting = Formatting.Indented;
                    xmlWriter.WriteStartDocument();
                    xmlWriter.WriteComment("XML document generated with UBW data");
                    xmlWriter.WriteStartElement("MasterDataObjects");

                    foreach (var bestiller in F1bestiller)
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