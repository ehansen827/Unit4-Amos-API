using System;
using System.Data;
using System.Threading.Tasks;
using A1AR.SVC.Worker.Lib.Common;
using Dapper;
using Fjord1.Int.API.Models.DB;
using Fjord1.Int.API.Services;
using Microsoft.Extensions.Logging;

namespace Fjord1.Int.API.Workers
{
    public class ReceivAmos : Worker<WorkerParameters, WorkerSettings>
    {
        private readonly WorkerSettings _settings;
        private readonly ILogger<Task> _workerLogger;
        private readonly IGetHttpClient _getHttpClient;

        public ReceivAmos(ILogger<Task> _workerLogger, WorkerSettings _settings, IGetHttpClient _getHttpClient)
        {
            this._settings = _settings;
            this._workerLogger = _workerLogger;
            this._getHttpClient = _getHttpClient;
        }

        public override async Task<JobResult> Execute(WorkerParameters parameters)
		{
            try
            {
                using IDbConnection dbConnectionAmos = _settings.AmosDbConnection.CreateConnection();
                dbConnectionAmos.Open();
                _workerLogger.LogInformation(dbConnectionAmos.ConnectionString);
                var SQLStringSelectPO = @"SELECT formno, cc.CostCentreCode as Account, ReceivedDate 
                                        FROM orderform a
                                        INNER JOIN CostCentre cc ON a.CostCentreID = cc.CostCentreID
                                        WHERE 1=1
                                        AND ReceivedDate IS NOT null
                                        AND cc.CostCentreCode != 'VH'
                                        AND a.lastupdated > @LastSuccessFulRun";

                var LastSuccessFulRun = LastSucessfulRun(this.JobInstance.Id).ToString("yyyy-MM-dd HH:mm");
                if (!string.IsNullOrEmpty(_settings.LastUpdated)) LastSuccessFulRun = _settings.LastUpdated;
                foreach (var value in dbConnectionAmos.Query<OrderForm>(SQLStringSelectPO, new { LastSuccessFulRun }))
                {
                    var OrderID = value.FormNo;
                    var Account = value.Account;
                    var ReceivedDate = value.ReceivedDate;
                    UpdateReceived(OrderID.ToString(), Account, ReceivedDate);
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
            using IDbConnection dbConnectionAmos = _settings.AmosDbConnection.CreateConnection();
            dbConnectionAmos.Open();
            _workerLogger.LogInformation("Updating order in A1AR_Apoready: " + OrderID);
            var SQLStringInsert = @"UPDATE A1AR_APOREADY
                                    SET account = @Account, receiveddate = @ReceivedDate
                                    WHERE accountable = 'AMOS' AND order_id = @OrderID";
            try
            {
                dbConnectionAmos.Execute(SQLStringInsert, new { OrderID, Account, ReceivedDate });
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
                                    FROM ATE.TaskInstances ti
                                    INNER JOIN ATE.TaskInstances td on td.TaskDefinitionId = ti.TaskDefinitionId 
                                    WHERE ti.result = 1 
                                    AND td.Id = @taskId";
            var res = dbConnectionATE.ExecuteScalar<DateTime>(SQLStringGetRun, new { taskId }).ToString("yyyy-MM-dd HH:mm");
            return Convert.ToDateTime(res);
        }
    }
}