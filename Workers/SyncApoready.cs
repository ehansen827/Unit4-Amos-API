using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using A1AR.SVC.Worker.Lib.Common;
using Dapper;
using Fjord1.Int.API.Services;
using Microsoft.Extensions.Logging;

/*************************************
 * 
 *  Worker designed to update apoready  
 *  with current order status from Amos
 * 
 *  Runs every 15 mins
 * 
 *************************************/

namespace Fjord1.Int.API.Workers
{
    public class SyncApoready : Worker<WorkerParameters, WorkerSettings>
    {
        private readonly WorkerSettings _settings;
        private readonly ILogger<Task> _workerLogger;
        private readonly IGetHttpClient _getHttpClient;

        public SyncApoready(ILogger<Task> _workerLogger, WorkerSettings _settings, IGetHttpClient _getHttpClient)
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
                // Test
                // {_settings.SQLInjection} = [SRFLOAMOSDBLAND].[AmosOffice].[Amos]
                // Production
                // {_settings.SQLInjection} = [SRFLOAMOSDB2019].[AmosOffice].[Amos]
                var SQLUpddate1 = $@"UPDATE apo 
                                    SET Status =
                                    CASE
				                        WHEN orf.WorkFlowStatusID in(6000000006) and v.FinalInvoice = 0 THEN 'O'
				                        WHEN orf.WorkFlowStatusID in(6000000006) and v.FinalInvoice = 1 THEN 'A'
				                        WHEN orf.WorkFlowStatusID in(6000000005) and v.FinalInvoice = 0 THEN 'O'
				                        WHEN orf.WorkFlowStatusID in(6000000005) and v.FinalInvoice = 1 THEN 'A'
				                        WHEN orf.WorkFlowStatusID in(6000000008) and v.FinalInvoice = 0 THEN 'O'
				                        WHEN orf.WorkFlowStatusID in(6000000008) and v.FinalInvoice = 1 THEN 'A'
                                    END
                                    FROM a1ar_apoready apo
                                    JOIN OrderForm orf ON CAST(apo.order_id AS varchar) = orf.FormNo
                                    JOIN WorkflowStatus ON orf.WorkFlowStatusID = WorkFlowStatus.StatusID
                                    JOIN Voucher v ON orf.OrderID = v.OrderID
                                    WHERE orf.FormType = 1
                                    AND (apo.invoice_no IS NULL OR apo.invoice_no = ' ')";
                var res1 = dbConnectionAmos.Execute(SQLUpddate1, commandTimeout: 60 * 60);
                _workerLogger.LogInformation("Active in Amos, not open in Apoready: " + res1);
            }
            catch (Exception ex)
            {
                return JobResult.Failed("Update failed: " + ex);
            }

            return JobResult.Success("OK");
        }
    }
}