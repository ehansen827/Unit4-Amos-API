using A1AR.SVC.Worker.Lib.Common;
using System;
using Dapper;
using System.Threading.Tasks;
using System.Data;
using System.Linq;
using Microsoft.Extensions.Logging;
using Fjord1.Int.API.Models.DB;

namespace Fjord1.Int.API.Workers
{
    public class OrderUBW : Worker, IWorkerSettings<WorkerSettings>
    {
        private readonly ILogger<Worker> _workerLogger;
        private readonly WorkerSettings _settings;
        public Type SettingsType => typeof(WorkerSettings);
        public OrderUBW(ILogger<Worker> workerLogger, WorkerSettings WorkerSettings)
        {
            _workerLogger = workerLogger;
            _settings = WorkerSettings;
        }

        public override async Task<JobResult> Execute()
        {
            var LastSuccessFulRun = LastSucessfulRun(this.JobInstance.Id).ToString("yyyy-MM-dd");
            if (!string.IsNullOrEmpty(_settings.LastUpdated)) LastSuccessFulRun = _settings.LastUpdated;
            _workerLogger.LogInformation("Lastupdated " + LastSuccessFulRun);

            using IDbConnection dbConnectionAgr = _settings.UBWDbConnection.CreateConnection();
            var SQLStringSelectUBWOrder = @"Select Order_id, Ext_ord_ref, Text1
                                            From Apoheader 
                                            Where responsible = 'AMOS' 
                                            and ext_ord_ref is Not Null
                                            and last_update >= @LastSuccessFulRun";

            var SQLStringSelectAmosOrder = @"Select FormNo
                                            From A1AR_OrderUBW
                                            Where formno = @Ext_ord_ref and orderid = @Order_id";

            var SQLStringInsertAmosOrder = @"Insert Into A1AR_OrderUBW (formno, orderid, Updated) values (@Ext_ord_ref, @Order_id, GETDATE())";

            var SQLStringUpdateAmosOrder = @"Update OrderForm
                                            Set Notes = IsNull(Notes,'- ') + CHAR(13)+CHAR(10) + 'Faktura ' + @Text1 + ' sendt til UBW med ordre nr. ' + @Order_id + ' @' + Cast(GETDATE() as Varchar(max)) 
                                            Where Formno = @Ext_ord_ref";

            var UBWOrders = dbConnectionAgr.Query<OrderInfo>(SQLStringSelectUBWOrder, new { LastSuccessFulRun}, commandTimeout: 1000);
            foreach (var UBWOrder in UBWOrders)
            {
                _workerLogger.LogInformation("Processing UBW order " + UBWOrder.Order_id + " for Amos order |" + UBWOrder.Ext_ord_ref + "| - invoice " + UBWOrder.Text1);
                using IDbConnection dbConnectionAmos = _settings.AmosDbConnection.CreateConnection();
                dbConnectionAmos.Open();
                var AmosOrder = dbConnectionAmos.Query<OrderInfo>(SQLStringSelectAmosOrder, new { UBWOrder.Ext_ord_ref, UBWOrder.Order_id }, commandTimeout: 1000);
                if(AmosOrder.Count() < 1)
                {
                    dbConnectionAmos.Execute(SQLStringInsertAmosOrder, new { UBWOrder.Ext_ord_ref, UBWOrder.Order_id }, commandTimeout: 1000);
                    dbConnectionAmos.Execute(SQLStringUpdateAmosOrder, new { UBWOrder.Ext_ord_ref, UBWOrder.Order_id, UBWOrder.Text1 }, commandTimeout: 1000);
                }
                else
                {
                    _workerLogger.LogInformation($"Amos ordre {UBWOrder.Ext_ord_ref} med UBW ordre {UBWOrder.Order_id} er registrert i databasen.");
                }
            }
        return JobResult.Success("OK");
        }
        public DateTime LastSucessfulRun(Guid taskId)
        {
            using IDbConnection dbConnectionATE = _settings.ATEDbConnection.CreateConnection();
            dbConnectionATE.Open();
            var SQLStringGetRun = @"SELECT max(ti.ExecutionFinish)
                                    FROM [ATE].[TaskInstances] ti
                                    inner join [ATE].[TaskInstances] td on td.TaskDefinitionId = ti.TaskDefinitionId 
                                    where ti.result = 1 
                                    and td.Id = @taskId";
            var res = dbConnectionATE.ExecuteScalar<DateTime>(SQLStringGetRun, new { taskId }).ToString("yyyy-MM-dd HH:mm");
            return Convert.ToDateTime(res);
        }
    }
}