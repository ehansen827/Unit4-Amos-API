using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using A1AR.SVC.Worker.Lib.Common;
using Dapper;
using Fjord1.Int.API.Models.DB;
using Fjord1.Int.API.Services;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Fjord1.Int.API.Workers
{
    public class PrActUBW : Worker<WorkerParameters, WorkerSettings>
    {
        private readonly WorkerSettings _settings;
        private readonly ILogger<Task> _workerLogger;
        private readonly IGetHttpClient _getHttpClient;

        public PrActUBW(ILogger<Task> _workerLogger, WorkerSettings _settings, IGetHttpClient _getHttpClient)
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
                _workerLogger.LogInformation("Connected to Amos, clearing repository...");
                var SQLStringDelete0 = "Delete from Amos.A1ATE_Accounts ";
                dbConnectionAmos.Execute(SQLStringDelete0);
                var SQLStringDelete = "Delete from Amos.A1ATE_ReposPA ";
                dbConnectionAmos.Execute(SQLStringDelete);

                var ubwClient = _getHttpClient.CreateUBW(_settings);
                // Process Accounts:
                var url = $"{_settings.ApiAccounts}";
                using (HttpResponseMessage dataresponse = await ubwClient.GetAsync(url))
                {
                    HttpContent content = dataresponse.Content;
                    var Json = await content.ReadAsStringAsync();
                    var accounts = JsonConvert.DeserializeObject<List<Accounts>>(Json);

                    if (accounts != null)
                    {
                        foreach (var account in accounts)
                        {
                            var SQLStringInsert = @"INSERT INTO A1ATE_Accounts (Account) 
                                                     VALUES (@Account)";
                            dbConnectionAmos.Execute(SQLStringInsert, new { account.Account });
                        }
                    }
                }

                // Process Projects:
                url = $"{_settings.ApiProjects}";
                using (HttpResponseMessage dataresponse = await ubwClient.GetAsync(url))
                {
                    HttpContent content = dataresponse.Content;
                    var Json = await content.ReadAsStringAsync();
                    var projects = JsonConvert.DeserializeObject<List<Projects>>(Json);

                    if (projects != null)
                    {
                        foreach (var project in projects)
                        {
                            string[] globProj = _settings.GlobalProjects;
                            for (int i = 0; i < globProj.Length; i++)
                            {
                                if (project.Installation == globProj[i]) project.Installation = "1";
                            }
                            var SQLStringInsert = @"INSERT INTO A1ATE_ReposPA (Project, ProjectDesc, Account, AccountDesc, Installation, Date_from, Date_to) 
                                                    VALUES (@Project, @ProjectDesc, @Account, @AccountDesc, @Installation, @DateFrom, @DateTo)";

                            dbConnectionAmos.Execute(SQLStringInsert, new { project.Project, project.ProjectDesc, project.Account, project.AccountDesc, project.Installation, project.DateFrom, project.DateTo });
                        }
                    }
                }

                _workerLogger.LogInformation("Executing SP Amos.A1ATE_SP_ReposPA...");
                var SQLStringExec = ("EXEC Amos.A1ATE_SP_ReposPA");
                dbConnectionAmos.Execute(SQLStringExec, commandTimeout: 60 * 60);
                _workerLogger.LogInformation("Executing SP Amos.A1ATE_DIST_VH...");
                SQLStringExec = ("EXEC Amos.A1ATE_DIST_VH");
                dbConnectionAmos.Execute(SQLStringExec, commandTimeout: 60 * 60);

                var GetValues = @"SELECT Project, Account FROM A1ATE_ReposPA WHERE date_from > GETDATE() OR date_to < GETDATE()";
                var SelectProjects = @"SELECT AccountCodeID FROM AccountCode WHERE code = @Project"; // PV19075
                var SelectAccounts = @"SELECT CostCentreID FROM CostCentre WHERE CostCentreCode = @Account"; // 1294
                var DeleteCCActs = @"DELETE FROM CostCentreAccount WHERE AccountCodeID = @projectid AND CostCentreID = @accountid"; // 6000015608 - 6000000011
                foreach (var value in dbConnectionAmos.Query<Aglrelvalue>(GetValues))
                {
                    foreach(var projectid in dbConnectionAmos.Query<long>(SelectProjects, new { value.Project }))
                    {
                        foreach(var accountid in dbConnectionAmos.Query<long>(SelectAccounts, new { value.Account }))
                        {
                            _workerLogger.LogInformation($"Deleting account {accountid} and project {projectid}...");
                            try
                            {
                            var rad =dbConnectionAmos.Execute(DeleteCCActs, new { accountid, projectid });
                            }
                            catch (Exception ex)
                            {
                                _workerLogger.LogError($"Error deleting account {accountid} and project {projectid}..." + ex.ToString());
                            }
                        }
                    }
                }
                dbConnectionAmos.Close();
            }
            catch (Exception ex)
            {
                _workerLogger.LogError(ex.ToString());
            }
            return JobResult.Success("OK");
        }
    }
}