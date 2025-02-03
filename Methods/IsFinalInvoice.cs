//using System;
//using System.Collections.Generic;
//using System.Data;
//using System.Text;
//using System.Threading.Tasks;
//using A1AR.SVC.Worker.Lib.Common;
//using Dapper;
//using Microsoft.Extensions.Logging;

//namespace Fjord1.Int.NetCore.Methods
//{
//    public class IsFinalInvoice : Worker, IWorkerSettings<WorkerSettings>
//	{
//        private readonly ILogger<Worker> _workerLogger;
//        private readonly WorkerSettings _settings;
//        public Type SettingsType => typeof(WorkerSettings);

//        public override Task<JobResult> Execute()
//        {

//            public bool IsFinalInvoice(string currency, double estimate, double amount, DateTime orderDate)
//            {
//                using IDbConnection dbConnectionUBW = _settings.UBWDbConnection.CreateConnection();
//                dbConnectionUBW.Open();
//                var tolerance = false;
//                var SQLStringGetCur = @"select exch_rate from acrexchrates
//                                        where currency = @currency and
//                                        date_from = (select max(date_from) from acrexchrates where currency = @currency)";
//                var res = dbConnectionUBW.ExecuteScalar(SQLStringGetCur, new { currency, orderDate });
//                if (res == null) res = "1";
//                var balance = (estimate * Convert.ToDouble(res) - (amount * Convert.ToDouble(res))); //100
//                if (estimate * Convert.ToDouble(res) * .05 > balance && balance <= 200) tolerance = true;
//                return tolerance;
//            }
//        }
//    }
//}
