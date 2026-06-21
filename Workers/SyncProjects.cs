using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using A1AR.SVC.Worker.Lib.Common;
using Dapper;
using Fjord1.Int.API.Models.DB;
using Fjord1.Int.API.Services;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

/*************************************
 * 
 *  Worker designed to update projects  
 *  with current order status from UBW
 * 
 *  Runs every day
 * 
 *************************************/

namespace Fjord1.Int.API.Workers
{
    public class SyncProjects : Worker<WorkerParameters, WorkerSettings>
    {
        private readonly WorkerSettings _settings;
        private readonly ILogger<Task> _workerLogger;
        private readonly IGetHttpClient _getHttpClient;

        public SyncProjects(ILogger<Task> _workerLogger, WorkerSettings _settings, IGetHttpClient _getHttpClient)
        {
            this._settings = _settings;
            this._workerLogger = _workerLogger;
            this._getHttpClient = _getHttpClient;
        }

        public override async Task<JobResult> Execute(WorkerParameters parameters)
        {
            using IDbConnection dbConnectionAmos = _settings.AmosDbConnection.CreateConnection();
            dbConnectionAmos.Open();

            try
            {
                var ubwClient = _getHttpClient.CreateUBW(_settings);

                var url = $"{_settings.ApiSyncProj}".TrimStart('/');
                HttpResponseMessage dataresponse = await ubwClient.GetAsync(url);
                HttpContent content = dataresponse.Content;
                var Json = await content.ReadAsStringAsync();
                var closedProjects = JsonConvert.DeserializeObject<List<SyncProj>>(Json);

                if (closedProjects != null)
                {
                    foreach (var project in closedProjects)
                    {
                        var SQLUpddate1 = $@"UPDATE ac
                                        SET active = 0
                                        FROM AccountCode ac
                                        WHERE ac.code = '{project.DimValue}'";
                        var res1 = dbConnectionAmos.Execute(SQLUpddate1, commandTimeout: 60 * 60);
                        _workerLogger.LogInformation("Projects disabled in Amos: " + res1);
                    }
                }
            }
            catch (Exception ex)
            {
                return JobResult.Failed("Update failed: " + ex);
            }

            return JobResult.Success("OK");
        }
    }
}