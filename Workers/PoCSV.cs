using System;
using System.Collections.Generic;
//using System.Data;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using A1AR.SVC.Worker.Lib.Common;
using Fjord1.Int.API.Models.DB;
//using CsvHelper;
//using CsvHelper.Configuration;
//using Dapper;
//using Fjord1.Int.API.Models.DB;
//using Fjord1.Int.API.Services;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Fjord1.Int.API.Workers
{
    public class PurchaseOrderDto
    {
        [JsonProperty("client")]
        public string Client { get; set; } = string.Empty;

        [JsonProperty("extOrdRef")]
        public string? ExtOrdRef { get; set; }

        [JsonProperty("orderId")]
        public long OrderId { get; set; }

        [JsonProperty("responsible")]
        public string Responsible { get; set; } = string.Empty;
    }

    public class Pocsv : Worker<WorkerParameters, WorkerSettings>
    {
        private readonly WorkerSettings _settings;
        private readonly ILogger<Task> _workerLogger;
        //private readonly IGetHttpClient _getHttpClient;

        public Pocsv(ILogger<Task> _workerLogger, WorkerSettings _settings)  //, IGetHttpClient _getHttpClient)
        {
            this._settings = _settings;
            this._workerLogger = _workerLogger;
            //this._getHttpClient = _getHttpClient;
        }

        public override async Task<JobResult> Execute(WorkerParameters parameters)
        {
            try
            {
                _workerLogger.LogInformation("Starting Pocsv worker...");
                //string apiUrl = _settings.apiBaseUrl + _settings.ApiPO;
                string apiUrl = "http://srflounit4tsapp/TESTAgressoM7-web-api/v1/objects/osgporderss";

                string username = _settings.UserNameUBW;
                string password = _settings.PasswordUBW;

                string outputDirectory = _settings.CsvDirectory;
                string outputFile = "PurchaseOrders.csv";

                // Create output directory if it doesn't exist
                Directory.CreateDirectory(outputDirectory);
                string outputPath = Path.Combine(outputDirectory, outputFile);

                // Basic Authentication
                using HttpClient client = new HttpClient();
                string credentials = $"{_settings.UserNameUBW}:{_settings.PasswordUBW}";
                string base64Credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));
                client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", base64Credentials);
                client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

                // Call Unit4 API
                _workerLogger.LogInformation("Calling Unit4 API...");
                //_workerLogger.LogInformation($"BASE URL: {client.BaseAddress}");
                //_workerLogger.LogInformation($"API URL: {_settings.ApiPO}");
                _workerLogger.LogInformation($"URL: {apiUrl}");

                HttpResponseMessage response = await client.GetAsync(apiUrl);
                if (!response.IsSuccessStatusCode)
                {
                    string errorResponse = await response.Content.ReadAsStringAsync();
                    _workerLogger.LogInformation($"API request failed: {(int)response.StatusCode} {response.ReasonPhrase}");
                    _workerLogger.LogInformation(errorResponse);
                    return JobResult.Failed("API request failed: " + errorResponse);
                }
                else
                {
                    _workerLogger.LogInformation($"API request succeeded: {(int)response.StatusCode} {response.ReasonPhrase}");
                }

                string jsonPayload = await response.Content.ReadAsStringAsync();

                List<PurchaseOrderDto> orders = JsonConvert.DeserializeObject<List<PurchaseOrderDto>>(jsonPayload);

                _workerLogger.LogInformation("starting streamwriter...");
                await using StreamWriter writer = new StreamWriter(
                    outputPath,
                    false,
                    new UTF8Encoding(false));

                // Metadata line
                await writer.WriteLineAsync("MetaData Version:2.6 Separator:; Endtag:EOF Type:Firewall");

                int count = 0;
                if (orders != null)
                {
                    foreach (var order in orders)
                    {
                        string poNumber = order.ExtOrdRef ?? string.Empty;

                        if (!string.IsNullOrWhiteSpace(poNumber))
                        {
                            await writer.WriteLineAsync($"PO;;{poNumber};;");
                        }
                        count++;
                    }
                }

                //EOF
                await writer.WriteLineAsync("EOF");
                writer.Flush();

                _workerLogger.LogInformation("");
                _workerLogger.LogInformation($"Purchase orders exported: {count}");
                _workerLogger.LogInformation($"Output file: {outputPath}");
            }
            catch (Exception ex)
            {
                _workerLogger.LogError(ex.ToString());
                return JobResult.Failed("Failed: " + ex.Message);
            }
            return JobResult.Success("OK");
        }
        static HttpClient Unit4PurchaseOrderService(string apiBaseUrl, string username, string password)
        {
            var httpClient = new HttpClient
            {
                BaseAddress = new Uri(apiBaseUrl)
            };

            // Set up Basic Authentication
            var authToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}"));
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authToken);
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            return httpClient;
        }
    }
}