using A1AR.SVC.Worker.Lib.Common;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using System.Linq;

namespace Fjord1.Int.NetCore
{
    public class MissingInv : Worker, IWorkerSettings<WorkerSettings>
	{
        private readonly ILogger<Worker> _workerLogger;
		private readonly WorkerSettings _settings;
        public Type SettingsType => typeof(WorkerSettings);

        public MissingInv(ILogger<Worker> workerLogger, WorkerSettings WorkerSettings)
		{
			_workerLogger = workerLogger;
			_settings = WorkerSettings;
        }

		public override async Task<JobResult> Execute()
		{
            var files = Directory.GetFiles(_settings.HoldingPath, "*.xml", SearchOption.TopDirectoryOnly);
  
            foreach (string file in files)
            {
                var filename = Path.GetFileName(file);
                var creation = File.GetCreationTime(file);

                using IDbConnection dbConnectionUBW = _settings.UBWDbConnection.CreateConnection();

                dbConnectionUBW.Open();
                var SQLStringSelectOrder = @"SELECT TOP 1 apo.order_id as Order_id, a.invoice_no FROM A1AR_APOREADY a
                                            JOIN apoheader apo ON CONVERT(varchar, a.order_id) = CONVERT(VARCHAR, apo.ext_ord_ref) AND CONVERT(VARCHAR, a.invoice_no) = CONVERT(VARCHAR, apo.text1)
                                            WHERE a.accountable = 'AMOS' AND a.filename = @filename";

                foreach (var Order in dbConnectionUBW.Query<A1ar_apoready>(SQLStringSelectOrder, new { filename }))
                {
                    if (!string.IsNullOrEmpty(Order.Order_id))
                    {
                        _workerLogger.LogInformation($"Invoice {Order.invoice_no} moved: {filename} from {creation} belongs to UBW order {Order.Order_id}.");
                        File.Move($"{_settings.HoldingPath}{filename}", $"{_settings.RootPath}{filename}");
                    }
                }
            }
            return JobResult.Success("OK");
        }
    }
}