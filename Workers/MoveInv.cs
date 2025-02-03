using A1AR.SVC.Worker.Lib.Common;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Data;
using System.Threading.Tasks;
using Dapper;

namespace Fjord1.Int.NetCore
{
    public class MoveInv : Worker, IWorkerSettings<WorkerSettings>
	{
        private readonly ILogger<Worker> _workerLogger;
		private readonly WorkerSettings _settings;
        public Type SettingsType => typeof(WorkerSettings);

        public MoveInv(ILogger<Worker> workerLogger, WorkerSettings WorkerSettings)
		{
			_workerLogger = workerLogger;
			_settings = WorkerSettings;
        }

		public override async Task<JobResult> Execute()
		{
            var delay = _settings.DelayLG04;
            var include = _settings.IncludeLG04;

            using IDbConnection dbConnectionAmos = _settings.AmosDbConnection.CreateConnection();
            try
            {
                dbConnectionAmos.Open();
                var SQLStringSelectOrder = @"Select FormNo, InvoiceNo 
                                            From [amos].[A1AR_OrderInvoice]
                                            Where date > DATEADD(DAY, @delay, CAST(GETDATE() AS Date)) and date < DATEADD(DAY, @include, CAST(GETDATE() AS Date))";
                var Orders = dbConnectionAmos.Query<OrderInvoice>(SQLStringSelectOrder, new { delay, include }, commandTimeout: 1000);
                using IDbConnection dbConnectionUBW = _settings.UBWDbConnection.CreateConnection();
                try
                {
                    dbConnectionUBW.Open();
                    var SQLStringSelectFilename = @"select filename from A1AR_APOREADY a
                                                    right join apoheader apo on convert(varchar, a.order_id) = convert(varchar, apo.ext_ord_ref) and convert(varchar, a.invoice_no) = convert(varchar, apo.text1)
                                                    Where a.accountable = 'AMOS' 
                                                    and a.Invoice_no = @InvoiceNo
                                                    and a.Order_id = @FormNo";
                    foreach (var Order in Orders)
                    {
                        var Filename = dbConnectionUBW.ExecuteScalar(SQLStringSelectFilename, new { Order.InvoiceNo, Order.FormNo }, commandTimeout: 1000);

                        if (Filename != null)
                        {
                            if (File.Exists($"{_settings.HoldingPath}{Path.GetFileName(Filename.ToString())}") && (!File.Exists($"{_settings.RootPath}{Path.GetFileName(Filename.ToString())}")))
                            {
                                _workerLogger.LogInformation("Moving invoice " + Filename);
                                File.Move($"{_settings.HoldingPath}{Filename}", $"{_settings.RootPath}{Filename}");
                                File.Copy($"{_settings.RootPath}{Filename}", $"{_settings.FraEHF}{Filename}");
                                _workerLogger.LogInformation("Done moving invoice " + Filename);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _workerLogger.LogError(ex.Message);
                    return JobResult.Failed("Failed to select filename from Apoready: " + ex.Message);
                }
            }
            catch (Exception ex)
            {
                _workerLogger.LogError(ex.Message);
                return JobResult.Failed("Failed select order from Orderinvoice: " + ex.Message);
            }
            return JobResult.Success("OK");
        }
    }
}