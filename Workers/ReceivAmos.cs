using A1AR.SVC.Worker.Lib.Common;
using Microsoft.Extensions.Logging;
using System;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Fjord1.Int.API.Models.DB;

namespace Fjord1.Int.API.Workers
{
    public class ReceivAmos : Worker, IWorkerSettings<WorkerSettings>
	{
        private readonly ILogger<Worker> _workerLogger;
		private readonly WorkerSettings _settings;
        public Type SettingsType => typeof(WorkerSettings);
        public ReceivAmos(ILogger<Worker> workerLogger, WorkerSettings WorkerSettings)
		{
			_workerLogger = workerLogger;
			_settings = WorkerSettings;
        }

		public override async Task<JobResult> Execute()
		{
            try
            {
                using IDbConnection dbConnectionAmos = _settings.AmosDbConnection.CreateConnection();
                dbConnectionAmos.Open();
                _workerLogger.LogInformation(dbConnectionAmos.ConnectionString);
                var SQLStringSelectPO = @"select formno, cc.CostCentreCode as Account, ReceivedDate 
                                        from orderform a
                                        inner join CostCentre cc on a.CostCentreID = cc.CostCentreID
                                        where 1=1
                                        and ReceivedDate is not null
                                        and cc.CostCentreCode != 'VH'
                                        and a.lastupdated > @LastSuccessFulRun";

                var LastSuccessFulRun = LastSucessfulRun(this.JobInstance.Id).ToString("yyyy-MM-dd HH:mm");
                if (!string.IsNullOrEmpty(_settings.LastUpdated)) LastSuccessFulRun = _settings.LastUpdated;
                foreach (var value in dbConnectionAmos.Query<OrderForm>(SQLStringSelectPO, new { LastSuccessFulRun }))
                {
                    var OrderID = value.FormNo;
                    var Account = value.Account;
                    var ReceivedDate = value.ReceivedDate;
                    UpdateReceived(OrderID, Account, ReceivedDate);
                }
            }
            catch (Exception ex)
            {
                _workerLogger.LogError(ex.ToString());
                return JobResult.Failed("Failed");
            }
            return JobResult.Success("OK");
        }
        private void UpdateReceived(string OrderID, string Account, DateTime ReceivedDate)
        {
            using IDbConnection dbConnectionUBW = _settings.UBWDbConnection.CreateConnection();
            dbConnectionUBW.Open();
            _workerLogger.LogInformation("Updating order in A1AR_Apoready: " + OrderID);
            var SQLStringInsert = @"UPDATE A1AR_APOREADY
                                    set account = @Account, receiveddate = @ReceivedDate
                                    where accountable = 'AMOS' and order_id = @OrderID";
            try
            {
                dbConnectionUBW.Execute(SQLStringInsert, new { OrderID, Account, ReceivedDate });
            }
            catch (Exception ex)
            {
                _workerLogger.LogError(ex.ToString());
            }
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