using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
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
    public class MissingInv : Worker<WorkerParameters, WorkerSettings>
    {
        private readonly WorkerSettings _settings;
        private readonly ILogger<Task> _workerLogger;
        private readonly IGetHttpClient _getHttpClient;

        public MissingInv(ILogger<Task> _workerLogger, WorkerSettings _settings, IGetHttpClient _getHttpClient)
        {
            this._settings = _settings;
            this._workerLogger = _workerLogger;
            this._getHttpClient = _getHttpClient;
        }

        public override async Task<JobResult> Execute(WorkerParameters parameters)
		{
            var files = Directory.GetFiles(_settings.HoldingPath, "*.xml", SearchOption.TopDirectoryOnly);
            var ubwClient = _getHttpClient.CreateUBW(_settings);

            foreach (string file in files)
            {
                var filename = Path.GetFileName(file);
                var creation = File.GetCreationTime(file);

                using IDbConnection dbConnectionAmos = _settings.AmosDbConnection.CreateConnection();
                dbConnectionAmos.Open();

                var SQLStringSelectOrder = @"SELECT TOP 1 apo.order_id as Order_id, a.invoice_no 
                                            FROM A1AR_APOREADY a
                                            WHERE a.accountable = 'AMOS' AND a.filename = @filename";

                var order = dbConnectionAmos.QuerySingle<A1ar_apoready>(SQLStringSelectOrder, new { filename });
                if (!string.IsNullOrEmpty(order.Order_id))
                {
                    var url = $"{_settings.ApiUBWOrder}'{order.Order_id}'".TrimStart('/');
                    HttpResponseMessage dataresponse = await ubwClient.GetAsync(url);
                    HttpContent content = dataresponse.Content;
                    var Json = await content.ReadAsStringAsync();
                    var value = JsonConvert.DeserializeObject<List<UBWOrder>>(Json);

                    if (value.Count() > 0)
                    {
                        _workerLogger.LogInformation($"Invoice {order.invoice_no} moved: {filename} from {creation} belongs to UBW order {order.Order_id}.");
                        File.Move($"{_settings.HoldingPath}{filename}", $"{_settings.RootPath}{filename}");
                    }
                }
            }
            return JobResult.Success("OK");
        }
    }
}