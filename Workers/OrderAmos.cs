using A1AR.SVC.Worker.Lib.Common;
using Microsoft.Extensions.Logging;
using System;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Fjord1.Int.API.Models.DB;

namespace Fjord1.Int.API.Workers
{
    public class OrderAmos : Worker, IWorkerSettings<WorkerSettings>
    {
        private readonly ILogger<Worker> _workerLogger;
        private readonly WorkerSettings _settings;
        public Type SettingsType => typeof(WorkerSettings);

        public OrderAmos(ILogger<Worker> workerLogger, WorkerSettings WorkerSettings)
        {
            _workerLogger = workerLogger;
            _settings = WorkerSettings;
        }

        public override  async  Task<JobResult> Execute()
        {
            try
            {
                using IDbConnection dbConnectionAmos = _settings.AmosDbConnection.CreateConnection();
                dbConnectionAmos.Open();
                // Registers new orders from Amos in A1AR_Apoready
                //var SQLStringSelectPO = @"Select OrderID, FormNo, OrderedDate, FormStatus, VendorId, uc.Name as Responsible, ua.Name as SuperInt, EstimatedTotal, InstCode, InstName, d.comment1 as Comment1, cc.CostCentreCode as Account  
                //                        From Orderform a 
                //      inner join amosuser uc on CreatedBy=uc.UserID
                //      inner join amosuser ua on SentBy=ua.UserID
                //      inner join department d on a.DeptID = d.DeptID
                //      inner join installation i on d.InstID = i.InstID 
                //                        inner join CostCentre cc on a.CostCentreID = cc.CostCentreID                  {_settings.SQLInjection}
                //                        Where Formtype = 1 and Formstatus = 1 
                //                        and OrderedDate is not null
                //                        and a.lastupdated > @LastSuccessFulRun";

                // Test
                // {_settings.SQLInjection} = [SRFLOUNIT4DB\UNIT4].[TestAgressoM7].[dbo]
                // Production
                // {_settings.SQLInjection} = [SRFLOUNIT4DB\UNIT4].[AgressoM7].[dbo]
                var SQLStringSelectPO = $@"SELECT OrderID, a.FormNo, OrderedDate, FormStatus, VendorId, uc.Name as Responsible, ua.Name as SuperInt, EstimatedTotal, i.InstCode, i.InstName, d.comment1 as Comment1, cc.CostCentreCode as Account  
                                        FROM Orderform a 
						                INNER JOIN amosuser uc ON CreatedBy = uc.UserID
						                INNER JOIN amosuser ua ON SentBy = ua.UserID
						                INNER JOIN department d ON a.DeptID = d.DeptID
						                INNER JOIN installation i ON d.InstID = i.InstID 
                                        INNER JOIN CostCentre cc ON a.CostCentreID = cc.CostCentreID
										LEFT JOIN {_settings.SQLInjection}.[a1ar_apoready] apor ON a.FormNo = CAST(apor.Order_id AS varchar(99))
                                        WHERE Formtype = 1 AND Formstatus = 1 
                                        AND OrderedDate IS NOT NULL
										AND apor.last_update IS NULL
                                        AND a.lastupdated > DATEADD(MINUTE, @DelayMins, @LastSuccessFulRun)"; //@LastSuccessFulRun

                var LastSuccessFulRun = LastSucessfulRun(this.JobInstance.Id).ToString("yyyy-MM-dd HH:mm");
                if (!string.IsNullOrEmpty(_settings.LastUpdated)) LastSuccessFulRun = _settings.LastUpdated;
                _workerLogger.LogInformation($"Henter ordre oppdatert siden  { LastSuccessFulRun} minus {_settings.DelayMins} minutter...");
                foreach (var value in dbConnectionAmos.Query<OrderForm>(SQLStringSelectPO, new { LastSuccessFulRun, _settings.DelayMins }))
                {
                    _workerLogger.LogInformation("Processing order " + value.FormNo + " for vendor " + value.VendorID);
                    var supplierOk = true;
                    var installationOk = true;
                    string[] exSupplier = _settings.ExcludeSupplier;
                    for (int i = 0; i < exSupplier.Length; i++)
                    {
                        if (value.VendorID.ToString() == exSupplier[i]) supplierOk = false;
                    }
                    string[] exInstallation = _settings.ExcludeInstallation;
                    for (int i = 0; i < exInstallation.Length; i++)
                    {
                        if (value.InstCode.ToString() == exInstallation[i]) installationOk = false;
                    }

                    var InstName = value.InstName;
                    var OrderID = value.OrderID;
                    var FormNo = value.FormNo;
                    var Status = value.FormStatus;
                    var OrderDate = value.OrderedDate;
                    var Vendor = value.VendorID;
                    var Responsible = value.Responsible;
                    var SuperInt = value.SuperInt;
                    var Amount = value.EstimatedTotal;
                    var Account = value.Account;

                    if (installationOk == true && supplierOk == true) InsertORD(FormNo, Vendor, Responsible, Amount, OrderDate, Account, InstName, SuperInt);
                }
                // Cancelled orders
                var SQLSelectCancelledPO = @"SELECT FormNo   
                                            FROM Orderform a 
                                            WHERE Formtype = 1 AND Formstatus = 5
                                            AND a.FormNo NOT LIKE '%-%'
                                            AND a.lastupdated > DATEADD(MINUTE, @DelayMins, @LastSuccessFulRun)";

                foreach (var value in dbConnectionAmos.Query<OrderForm>(SQLSelectCancelledPO, new { LastSuccessFulRun, _settings.DelayMins }))
                {
                    _workerLogger.LogInformation("Processing cancelled order " + value.FormNo);
                    CancelORD(value.FormNo);
                }
                // Opened orders
                var SQLSelectOpenedPO = @"Select FormNo, WorkFlowStatusID  
                                        From Orderform a 
                                        Where Formtype = 1 and Formstatus = 1
                                        and a.lastupdated > DATEADD(MINUTE, -20, @LastSuccessFulRun)";
                var InsertOpenedPO = @"INSERT INTO A1ARReopenedPO (Formno, workflowstatusid, Updated)
                                        VALUES (@Formno, @workflowstatusid, GETDATE())";

                foreach (var order in dbConnectionAmos.Query<OrderForm>(SQLSelectOpenedPO, new { LastSuccessFulRun, _settings.DelayMins }))
                {
                    _workerLogger.LogInformation("Processing opened order " + order.FormNo);
                    OpenedORD(order.FormNo);
                    dbConnectionAmos.Execute(InsertOpenedPO, new { order.FormNo, order.WorkFlowStatusID });
                }
            }
            catch (Exception ex)
            {
                _workerLogger.LogError(ex.ToString());
                return JobResult.Failed("Failed: " + ex.Message);  
            }
            return JobResult.Success("OK");
        }
        private JobResult InsertORD(string purchaseOrderNo, double apar_id, string responsible, double amount, DateTime orderDate, string Account, string InstName, string SuperInt)
        {
            using IDbConnection dbConnectionUBW = _settings.UBWDbConnection.CreateConnection();
            dbConnectionUBW.Open();
            _workerLogger.LogInformation("Inserting order into A1AR_Apoready: " + purchaseOrderNo);
            var SQLStringInsert = $@"Merge a1ar_apoready T
                                    Using (select order_id from a1ar_apoready Where Accountable = 'AMOS' and Order_id = @order_id union select @order_id) as S
                                    on (T.Accountable = 'AMOS' and T.order_id = @order_id and (LEN(filename) < 2 or filename is null))
                                    When Matched Then 
	                                    Update set t.last_update = getdate(), t.estimated_amount = @amount, t.order_date = @orderdate, t.superint = @superint, t.status = 'O' 
                                    When not Matched by target Then 
	                                    Insert (accountable, order_id,  status, last_update, client, apar_id,  responsible,  estimated_amount, order_date, account,  instname,  superint)
                                        Values('AMOS',      @order_id, 'O',    getdate(),  '50',    @apar_id, @responsible, @amount,          @orderdate, @Account, @instname, @superint);";

            try
            {
                dbConnectionUBW.Execute(SQLStringInsert, new { order_id = purchaseOrderNo, apar_id, responsible, amount, orderdate = orderDate, Account, InstName, SuperInt });
            }
            catch (Exception ex)
            {
                _workerLogger.LogError(ex.ToString());
                return JobResult.Failed("Insert order failed: " + ex.Message);
            }
            return JobResult.Success("Insert order Ok");
        }
        private JobResult CancelORD(string purchaseOrderNo)
        {
            using IDbConnection dbConnectionUBW = _settings.UBWDbConnection.CreateConnection();
            dbConnectionUBW.Open();
            _workerLogger.LogInformation("Cancelling order in A1AR_Apoready: " + purchaseOrderNo);
            var SQLStringUpdate = @"Update A1AR_APOREADY
                                    Set Status = 'C'
                                    where ORDER_ID = @order_id";
            try
            {
                dbConnectionUBW.Execute(SQLStringUpdate, new { order_id = purchaseOrderNo });
            }
            catch (Exception ex)
            {
                _workerLogger.LogError(ex.ToString());
                return JobResult.Failed("Cancel order failed: " + ex.Message);
            }
            return JobResult.Success("Cancel order Ok");
        }
        private JobResult OpenedORD(string purchaseOrderNo)
        {
            using IDbConnection dbConnectionUBW = _settings.UBWDbConnection.CreateConnection();
            dbConnectionUBW.Open();
            _workerLogger.LogInformation("Opening order in A1AR_Apoready: " + purchaseOrderNo);
            var SQLStringUpdate = @"UPDATE A1AR_APOREADY
                                    SET Status = 'O'
                                    WHERE ORDER_ID = @order_id AND INVOICE_NO = ' '";
            try
            {
                dbConnectionUBW.Execute(SQLStringUpdate, new { order_id = purchaseOrderNo });
            }
            catch (Exception ex)
            {
                _workerLogger.LogError(ex.ToString());
                return JobResult.Failed("Open order failed: " + ex.Message);
            }
            return JobResult.Success("Open order Ok");
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