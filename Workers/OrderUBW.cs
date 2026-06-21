using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using A1AR.SVC.Worker.Lib.Common;
using Dapper;
using Fjord1.Int.API.Models.DB;
using Fjord1.Int.API.Services;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Fjord1.Int.API.Workers
{
    public class OrderUBW : Worker<WorkerParameters, WorkerSettings>
    {
        private readonly WorkerSettings _settings;
        private readonly ILogger<Task> _workerLogger;
        private readonly IGetHttpClient _getHttpClient;

        public OrderUBW(ILogger<Task> _workerLogger, WorkerSettings _settings, IGetHttpClient _getHttpClient)
        {
            this._settings = _settings;
            this._workerLogger = _workerLogger;
            this._getHttpClient = _getHttpClient;
        }

        public override async Task<JobResult> Execute(WorkerParameters parameters)
        {
            var LastSuccessfulRun = LastSucessfulRun(this.JobInstance.Id).ToString("yyyy-MM-dd");
            if (!string.IsNullOrEmpty(_settings.LastUpdated)) LastSuccessfulRun = _settings.LastUpdated;
            _workerLogger.LogInformation("Lastupdated " + LastSuccessfulRun);

            var SQLStringSelectAmosOrder = @"SELECT FormNo
                                            FROM A1AR_OrderUBW
                                            WHERE formno = @Ext_ord_ref AND orderid = @Order_id";

            var SQLStringInsertAmosOrder = @"INSERT INTO A1AR_OrderUBW (formno, orderid, Updated) VALUES (@Ext_ord_ref, @Order_id, GETDATE())";

            var SQLStringUpdateAmosOrder = @"UPDATE OrderForm
                                            SET Notes = IsNull(Notes,'- ') + CHAR(13)+CHAR(10) + 'Faktura ' + @Text1 + ' sendt til UBW med ordre nr. ' + @Order_id + ' @' + Cast(GETDATE() AS Varchar(max)) 
                                            WHERE Formno = @Ext_ord_ref";

            var ubwClient = _getHttpClient.CreateUBW(_settings);

            var url = $"{_settings.ApiUBWOrder}{LastSuccessfulRun}".TrimStart('/');
            HttpResponseMessage dataresponse = await ubwClient.GetAsync(url);
            HttpContent content = dataresponse.Content;
            var Json = await content.ReadAsStringAsync();
            var UBWOrders = JsonConvert.DeserializeObject<List<OrderInfo>>(Json);

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
                                    FROM ATE.TaskInstances ti
                                    INNER JOIN ATE.TaskInstances td on td.TaskDefinitionId = ti.TaskDefinitionId 
                                    WHERE ti.result = 1 
                                    AND td.Id = @taskId";
            var res = dbConnectionATE.ExecuteScalar<DateTime>(SQLStringGetRun, new { taskId }).ToString("yyyy-MM-dd HH:mm");
            return Convert.ToDateTime(res);
        }
    }
}