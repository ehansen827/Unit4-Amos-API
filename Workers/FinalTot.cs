using A1AR.SVC.Worker.Lib.Common;
using System;
using Dapper;
using System.Threading.Tasks;
using System.Data;
using Microsoft.Extensions.Logging;
using Fjord1.Int.API.Models.DB;

namespace Fjord1.Int.API.Workers
{
    public class FinalTot : Worker, IWorkerSettings<WorkerSettings>
    {
        private readonly ILogger<Worker> _workerLogger;
        private readonly WorkerSettings _settings;
        public Type SettingsType => typeof(WorkerSettings);
        public FinalTot(ILogger<Worker> workerLogger, WorkerSettings WorkerSettings)
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
                var LastSuccessFulRun = LastSucessfulRun(this.JobInstance.Id).ToString("yyyy-MM-dd HH:mm");
                if (!string.IsNullOrEmpty(_settings.LastUpdated)) LastSuccessFulRun = _settings.LastUpdated;

                var SQLSelectPO = @"Select FormNo
                                    from orderform a
                                    inner join voucher v on a.OrderID = v.orderid
                                    where(a.ActualTotal = 0 and a.PartPayment = 0) and v.amount != 0 and a.LastUpdated > @LastSuccessFulRun";
                foreach (var porder in await dbConnectionAmos.QueryAsync<OrderForm>(SQLSelectPO, new { LastSuccessFulRun })) 
                {
                _workerLogger.LogInformation($"Updating {porder.FormNo} with missing payment information.");
                } 
                var SQLUpdatePO = @"update a
                                    set PartPayment = v.amount  
                                    from orderform as a
                                    inner join voucher v on a.OrderID = v.orderid
                                    where PartPayment=0 and ActualTotal=0 and v.Amount < a.EstimatedTotal
                                    and a.LastUpdated > @LastSuccessFulRun

                                    update a
                                    set a.ActualTotal = v.amount  
                                    from orderform as a
                                    inner join voucher v on a.OrderID = v.orderid
                                    where PartPayment=0 and ActualTotal=0 and v.Amount >= a.EstimatedTotal
                                    and a.LastUpdated > @LastSuccessFulRun";

                await dbConnectionAmos.ExecuteAsync(SQLUpdatePO, new { LastSuccessFulRun });
            }
            catch (Exception ex)
            {
                _workerLogger.LogInformation(ex.Message);
                return JobResult.Failed("Failed");
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