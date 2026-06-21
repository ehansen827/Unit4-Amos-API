using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Reflection.PortableExecutable;
using System.Threading.Tasks;
using A1AR.SVC.Worker.Lib.Common;
using Dapper;
using Fjord1.Int.API.Models.DB;
using Fjord1.Int.API.Services;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Fjord1.Int.API.Workers
{
    public class MoveInv : Worker<WorkerParameters, WorkerSettings>
    {
        private readonly WorkerSettings _settings;
        private readonly ILogger<Task> _workerLogger;
        private readonly IGetHttpClient _getHttpClient;

        public MoveInv(ILogger<Task> _workerLogger, WorkerSettings _settings, IGetHttpClient _getHttpClient)
        {
            this._settings = _settings;
            this._workerLogger = _workerLogger;
            this._getHttpClient = _getHttpClient;
        }

        public override async Task<JobResult> Execute(WorkerParameters parameters)
		{
            var delay = _settings.DelayLG04;
            var include = _settings.IncludeLG04;

            using IDbConnection dbConnectionAmos = _settings.AmosDbConnection.CreateConnection();
            try
            {
                //delay = -7;
                //include = 1;
                dbConnectionAmos.Open();
                var SQLStringSelectOrder = @"SELECT apr.filename AS Filename, oi.formno AS FormNo, oi.invoiceno AS InvoiceNo
                                            FROM A1AR_OrderInvoice oi
                                            JOIN a1ar_apoready apr ON CAST(apr.order_id AS varchar) = CAST(oi.formno AS varchar) AND apr.invoice_no = oi.invoiceno
                                            WHERE oi.date > DATEADD(DAY, @delay, CAST(GETDATE() AS Date)) AND oi.date < DATEADD(DAY, @include, CAST(GETDATE() AS Date))";
                var Orders = dbConnectionAmos.Query<OrderInvoice>(SQLStringSelectOrder, new { delay, include }, commandTimeout: 1000);
                var ubwClient = _getHttpClient.CreateUBW(_settings);
                try
                {
                    foreach (var Order in Orders)
                    {
                        var url = $"{_settings.ApiMoveInv}'{Order.FormNo}'%20and%20text1%20eq%20'{Order.InvoiceNo}'".TrimStart('/');
                        HttpResponseMessage dataresponse = await ubwClient.GetAsync(url);
                        HttpContent content = dataresponse.Content;
                        var Json = await content.ReadAsStringAsync();
                        var values = JsonConvert.DeserializeObject<List<OrderInvoice>>(Json);

                        if (values != null)
                        {
                            if (File.Exists($"{_settings.HoldingPath}{Path.GetFileName(Order.Filename.ToString())}") && (!File.Exists($"{_settings.RootPath}{Path.GetFileName(Order.Filename.ToString())}")))
                            {
                                _workerLogger.LogInformation("Moving invoice " + Order.Filename);
                                File.Move($"{_settings.HoldingPath}{Order.Filename}", $"{_settings.RootPath}{Order.Filename}");
                                File.Copy($"{_settings.RootPath}{Order.Filename}", $"{_settings.FraEHF}{Order.Filename}");
                                _workerLogger.LogInformation("Done moving invoice " + Order.Filename);
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