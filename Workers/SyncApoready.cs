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

namespace Fjord1.Int.API.Workers
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
	                                    --WHEN orf.formstatus = 3 and v.finalinvoice = 1 THEN 'A' 
	                                    --WHEN orf.formstatus = 3 and v.finalinvoice = 0 THEN 'O' 
	                                    --WHEN orf.formstatus = 1 and v.finalinvoice = 1 THEN 'A' 
	                                    --WHEN orf.formstatus = 1 and v.finalinvoice = 0 THEN 'O' 
                                    END
                                    FROM a1ar_apoready apo
                                    JOIN {_settings.SQLInjection}.[OrderForm] orf ON CAST(apo.order_id AS varchar) = orf.FormNo
                                    JOIN {_settings.SQLInjection}.[WorkflowStatus] ON orf.WorkFlowStatusID = WorkFlowStatus.StatusID
                                    JOIN {_settings.SQLInjection}.[Voucher] v ON orf.OrderID = v.OrderID
                                    WHERE orf.FormType = 1
                                    AND (apo.invoice_no IS NULL OR apo.invoice_no = ' ')";
                var res1 = dbConnectionUBW.Execute(SQLUpddate1, commandTimeout: 60 * 60);
                //_workerLogger.LogInformation("Active in Amos, not open in Apoready: " + res1);

            }
            catch (Exception ex)
            {
                return JobResult.Failed("Update failed: " + ex);
            }

            return JobResult.Success("OK");
        }
    }
}