using A1AR.SVC.Worker.Lib.Common;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using System.Linq;

/*************************************
 * 
 *  Worker designed to update apoready  
 *  with current order status from Amos
 * 
 *  Runs every 15 mins
 * 
 *************************************/

namespace Fjord1.Int.NetCore
{
    public class SyncApoready : Worker, IWorkerSettings<WorkerSettings>
    {
        private readonly ILogger<Worker> _workerLogger;
        private readonly WorkerSettings _settings;
        public Type SettingsType => typeof(WorkerSettings);

        public SyncApoready(ILogger<Worker> workerLogger, WorkerSettings WorkerSettings)
        {
            _workerLogger = workerLogger;
            _settings = WorkerSettings;
        }

        public override async Task<JobResult> Execute()
        {
            using IDbConnection dbConnectionUBW = _settings.UBWDbConnection.CreateConnection();
            dbConnectionUBW.Open();

            try
            {
                var SQLUpddate1 = $@"UPDATE apo
                                SET apo.status = 'O'
                                FROM a1ar_apoready apo
                                JOIN {_settings.SQLInjection} orf ON orf.FormNo = CAST(apo.order_id AS varchar)
                                WHERE 1=1
                                AND FormType = 1 AND FormStatus = 1
                                AND apo.status != 'O' AND apo.Filename IS NULL";
                var res1 = dbConnectionUBW.Execute(SQLUpddate1);
                _workerLogger.LogInformation("Active in Amos, not open in Apoready: " + res1);

                var SQLUpddate2 = $@"UPDATE apo
                                SET apo.status = 'A'
                                FROM a1ar_apoready apo
                                JOIN {_settings.SQLInjection} orf ON orf.FormNo = CAST(apo.order_id AS varchar)
                                WHERE 1=1
                                AND orf.FormType = 1 AND orf.FormStatus != 1
                                AND apo.status = 'O' AND apo.Filename IS NULL";
                var res2 = dbConnectionUBW.Execute(SQLUpddate2);
                _workerLogger.LogInformation("Not active in Amos, open in Apoready: " + res2);
            }
            catch (Exception ex)
            {
                return JobResult.Failed("Update failed: " + ex);
            }

            return JobResult.Success("OK");
        }
    }
}