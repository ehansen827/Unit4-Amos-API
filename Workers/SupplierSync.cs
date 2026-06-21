using System;
using System.Collections.Generic;
using System.Data;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using A1AR.SVC.Worker.Lib.Common;
using Dapper;
using Fjord1.Int.API.Models.DB;
using Fjord1.Int.API.Services;
using Fjord1.Int.API.Utilities;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Fjord1.Int.API.Workers    
{
    public class SupplierSync : Worker<WorkerParameters, WorkerSettings>
    {
        private readonly WorkerSettings _settings;
        private readonly ILogger<Task> _workerLogger;
        private readonly IGetHttpClient _getHttpClient;

        public SupplierSync(ILogger<Task> _workerLogger, WorkerSettings _settings, IGetHttpClient _getHttpClient)
        {
            this._settings = _settings;
            this._workerLogger = _workerLogger;
            this._getHttpClient = _getHttpClient;
        }

        public override async Task<JobResult> Execute(WorkerParameters parameters)
        {
            try
            {
                var LastSuccessfulRun = "2025-05-31";
                //var LastSuccessfulRun = _settings.LastSucessfulRun(this.JobInstance.Id).ToString("yyyy-MM-dd HH:mm");
                _workerLogger.LogInformation($"Last updated: {LastSuccessfulRun}");
                if (!string.IsNullOrEmpty(_settings.LastUpdated)) LastSuccessfulRun = _settings.LastUpdated;
                if (_settings.FullSync is true) LastSuccessfulRun = "1999-01-01";
                _workerLogger.LogInformation($"Synchronizing supplier information updated since " + LastSuccessfulRun);

                var ubwClient = _getHttpClient.CreateUBW(_settings);
                var url = $"{_settings.ApiSuppSync}{LastSuccessfulRun}".TrimStart('/'); 
                HttpResponseMessage dataresponse = await ubwClient.GetAsync(url);
                HttpContent content = dataresponse.Content;
                var Json2 = await content.ReadAsStringAsync();
                var suppliers = JsonConvert.DeserializeObject<List<SyncSup>>(Json2);

                if (suppliers != null)
                {
                    var SQLUpdateAdr1 = @"UPDATE Address
                                    SET gradeid = @gradeid
                                    WHERE addressid = @apar_id ";
                    var SQLUpdateAdr2 = @"UPDATE Address
                                    SET portalID = 3000001, Outputformat = 4
                                    WHERE addressid = @apar_id AND portalID IS NULL";
                    var gradeid = 0;

                    using IDbConnection dbConnectionAmos = _settings.AmosDbConnection.CreateConnection();
                    dbConnectionAmos.Open();

                    foreach (var supplier in suppliers)
                    {
                        _workerLogger.LogInformation("Updating supplier " + supplier.SupplierName);
                        if (supplier.Status == "N") gradeid = 1000001;
                        if (supplier.Status == "P") gradeid = 1000002;
                        if (supplier.Status == "C") gradeid = 1000003;

                        dbConnectionAmos.Execute(SQLUpdateAdr1, new { supplier.SupplierId, gradeid });
                        dbConnectionAmos.Execute(SQLUpdateAdr2, new { supplier.SupplierId });
                    }
                    dbConnectionAmos.Close();
                }
            }
            catch (Exception ex)
            {
                _workerLogger.LogError(ex.ToString());
                return JobResult.Failed("Failed: " + ex.Message);
            }
            return JobResult.Success("OK");
        }
    }
}