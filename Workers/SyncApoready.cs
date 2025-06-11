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
                                    SET Status =
                                    CASE
	                                    WHEN orf.formstatus = 3 and v.finalinvoice = 1 THEN 'A' 
	                                    WHEN orf.formstatus = 3 and v.finalinvoice = 0 THEN 'O' 
	                                    WHEN orf.formstatus = 1 and v.finalinvoice = 1 THEN 'A' 
	                                    WHEN orf.formstatus = 1 and v.finalinvoice = 0 THEN 'O' 
                                    END
                                    FROM a1ar_apoready apo
                                    JOIN [SRFLOAMOSDB2019].[AmosOffice].[amos].[OrderForm] orf ON CAST(apo.order_id AS varchar) = orf.FormNo
                                    JOIN [SRFLOAMOSDB2019].[AmosOffice].[amos].[VoucherOrderForm] vo ON orf.OrderID = vo.orderid
                                    JOIN [SRFLOAMOSDB2019].[AmosOffice].[amos].[Voucher] v ON vo.VoucherID = v.VoucherID
                                    WHERE orf.FormType = 1
                                    AND (apo.invoice_no IS NULL OR apo.invoice_no = ' ')";
                var res1 = dbConnectionUBW.Execute(SQLUpddate1, commandTimeout: 60 * 60);
                //_workerLogger.LogInformation("Active in Amos, not open in Apoready: " + res1); 110625

                //var SQLUpddate2 = $@"UPDATE apo
                //                SET apo.status = 'A'
                //                FROM a1ar_apoready apo
                //                JOIN {_settings.SQLInjection} orf ON orf.FormNo = CAST(apo.order_id AS varchar)
                //                WHERE 1=1
                //                AND orf.FormType = 1 AND orf.FormStatus != 1
                //                AND apo.status = 'O' AND apo.Filename IS NULL";
                //var res2 = dbConnectionUBW.Execute(SQLUpddate2, commandTimeout: 60 * 60);
                //_workerLogger.LogInformation("Not active in Amos, open in Apoready: " + res2);
            }
            catch (Exception ex)
            {
                return JobResult.Failed("Update failed: " + ex);
            }

            return JobResult.Success("OK");
        }
    }
}