using A1AR.SVC.Worker.Lib.Common;
using Microsoft.Extensions.Logging;
using System;
using System.Data;
using System.Threading.Tasks;
using Dapper;

namespace Fjord1.Int.NetCore
{
    public class SupplierSync : Worker, IWorkerSettings<WorkerSettings>
    {
        private readonly ILogger<Worker> _workerLogger;
        private readonly WorkerSettings _settings;
        public Type SettingsType => typeof(WorkerSettings);

        public SupplierSync(ILogger<Worker> workerLogger, WorkerSettings WorkerSettings)
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

                var LastSuccessFulRun = _settings.LastSucessfulRun(this.JobInstance.Id).ToString("yyyy-MM-dd HH:mm");
                _workerLogger.LogInformation($"Last updated: {LastSuccessFulRun}");
                if (!string.IsNullOrEmpty(_settings.LastUpdated)) LastSuccessFulRun = _settings.LastUpdated;
                if (_settings.FullSync is true) LastSuccessFulRun = "1999-01-01";
                _workerLogger.LogInformation($"Synchronizing supplier information updated since " + LastSuccessFulRun);

                var SQLSelectSup = @"SELECT apar_id, apar_name, asu.status, amosadr.addressid, amosadr.name, amosadr.gradeid, amosgra.descr,
                                    CASE
                                     WHEN amosadr.addressid is null THEN 'Yes' ELSE 'No'
                                    END AS New, 
                                    CASE	
                                     WHEN status = 'N' AND amosgra.gradeid != 1000001 THEN 'Yes' 
                                     WHEN status = 'P' AND amosgra.gradeid != 1000002 THEN 'Yes' 
                                     WHEN status = 'C' AND amosgra.gradeid != 1000003 THEN 'Yes' 
                                     ELSE 'No' 
                                    END AS Updated 
                                    FROM [dbo].[asuheader] asu 
                                    LEFT JOIN [srfloamosdb2019].[AmosOffice].[amos].[Address] amosadr ON asu.apar_id = amosadr.addressid
                                    LEFT JOIN [srfloamosdb2019].[AmosOffice].[amos].qagrading amosgra ON amosadr.gradeid = amosgra.gradeid
                                    WHERE client in ('50')
                                    AND asu.last_update >= @LastSuccessFulRun
                                    ";

                #region Connection to test:
                //var SQLSelectSup = @"SELECT apar_id, apar_name, asu.status, amosadr.addressid, amosadr.name, amosadr.gradeid, amosgra.descr,
                //                    CASE
                //                     WHEN amosadr.addressid is null THEN 'Yes' ELSE 'No'
                //                    END AS New, 
                //                    CASE	
                //                     WHEN status = 'N' AND amosgra.gradeid != 1000001 THEN 'Yes' 
                //                     WHEN status = 'P' AND amosgra.gradeid != 1000002 THEN 'Yes' 
                //                     WHEN status = 'C' AND amosgra.gradeid != 1000003 THEN 'Yes' 
                //                     ELSE 'No' 
                //                    END AS Updated 
                //                    FROM [dbo].[asuheader] asu 
                //                    LEFT JOIN [TAmosDB].[Amos93].[amos].[Address] amosadr ON asu.apar_id = amosadr.addressid
                //                    LEFT JOIN [TAmosDB].[Amos93].[amos].qagrading amosgra ON amosadr.gradeid = amosgra.gradeid
                //                    WHERE client = '50' 
                //                    AND asu.last_update >= @LastSuccessFulRun
                //                    "; 
                #endregion

                var SQLUpdateAdr = @"UPDATE Address
                                    SET gradeid = @gradeid
                                    WHERE addressid = @apar_id ";
                var SQLUpdateAdr2 = @"UPDATE Address
                                    SET portalID = 3000001, Outputformat = 4
                                    WHERE addressid = @apar_id AND portalID IS NULL";

                var gradeid = 0;

                foreach (var value in dbConnectionUBW.Query<SyncSup>(SQLSelectSup, new { LastSuccessFulRun }))
                {
                    _workerLogger.LogInformation("Processing supplier " + value.apar_name );

                    if(value.Updated == "Yes")
                    {
                        using IDbConnection dbConnectionAmos = _settings.AmosDbConnection.CreateConnection();
                        dbConnectionAmos.Open();

                        _workerLogger.LogInformation("Updating supplier " + value.apar_name);
                        if (value.status == "N") gradeid = 1000001;
                        if (value.status == "P") gradeid = 1000002;
                        if (value.status == "C") gradeid = 1000003;

                        dbConnectionAmos.Execute(SQLUpdateAdr, new { value.apar_id, gradeid });
                        dbConnectionAmos.Execute(SQLUpdateAdr2, new { value.apar_id, gradeid });
                        dbConnectionAmos.Close();
                    }
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