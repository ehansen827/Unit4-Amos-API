using A1AR.SVC.Worker.Lib.Common;
using Microsoft.Extensions.Logging;
using System;
using System.Data;
using System.Threading.Tasks;
using Dapper;

namespace Fjord1.Int.NetCore
{
    public class PrActUBW : Worker, IWorkerSettings<WorkerSettings>
	{
        private readonly ILogger<Worker> _workerLogger;
		private readonly WorkerSettings _settings;
        public Type SettingsType => typeof(WorkerSettings);
        private static string AmosAccount; 
        public PrActUBW(ILogger<Worker> workerLogger, WorkerSettings WorkerSettings)
		{
			_workerLogger = workerLogger;
			_settings = WorkerSettings;
        }

		public override Task<JobResult> Execute()
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

                using IDbConnection dbConnectionUBW = _settings.UBWDbConnection.CreateConnection();
                dbConnectionUBW.Open();
                _workerLogger.LogInformation("Connected to UBW, fetching data...");
                var SQLStringSelect0 = @"Select Ac.account as Account
                                        From aglaccounts Ac
                                        Inner Join aglrules R
                                        on ac.account_rule = R.Account_rule
                                        Where ac.CLIENT = '50' and R.client='50'
                                        And ac.PERIOD_TO > Convert(nvarchar(6),GETDATE(), 112)
                                        And R.dim_2_flag = 'M'
                                        And ac.account like ('%@Account%')
                                        Order by ac.account";

                var SQLStringSelect1 = @"Select agldv.dim_value as Project,
                                        Substring(agld1.description, 1, 40) as ProjectDesc,
                                        aglrv1.rel_value as Account,   
                                        Substring(agld2.description, 1, 40) as AccountDesc, 
                                        aglrv2.rel_value as Installation,
                                        pr.date_from as DateFrom, 
										case
											when pr.date_to < pr.org_date_to then pr.date_to 
											when pr.date_to >= pr.org_date_to then pr.org_date_to 
										end as DateTo
                                        from agldimvalue agldv
                                        join aglrelvalue aglrv1 on agldv.dim_value = aglrv1.att_value and aglrv1.rel_attr_id = 'A0' and agldv.client = aglrv1.client
                                        join aglrelvalue aglrv2 on agldv.dim_value = aglrv2.att_value and aglrv2.rel_attr_id = 'C1' and agldv.client = aglrv2.client
                                        join aglrelvalue aglrv3 on agldv.dim_value = aglrv3.att_value and aglrv3.rel_attr_id = 'Z11' and agldv.client = aglrv3.client
                                        join agldescription agld1 on agldv.dim_value = agld1.dim_value and agld1.attribute_id = aglrv1.attribute_id and agldv.client = agld1.client
                                        join agldescription agld2 on aglrv1.rel_value = agld2.dim_value and agld2.attribute_id = 'A0'and agldv.client = agld2.client
                                        join atsproject pr on agldv.dim_value = pr.head_project
                                        where 1=1
                                        and agldv.status = 'N'
                                        and aglrv3.rel_value='J'
                                        order by aglrv1.rel_value";

                foreach (var value in dbConnectionUBW.Query<Aglrelvalue>(SQLStringSelect0, new { _settings.Account }))
                {
                    AmosAccount = value.Account;
                    var SQLStringInsert0 = @"Insert Into Amos.A1ATE_Accounts (Account) 
                                            Values (@AmosAccount)";
                    dbConnectionAmos.Execute(SQLStringInsert0, new { AmosAccount });

                }
                foreach (var value in dbConnectionUBW.Query<Aglrelvalue>(SQLStringSelect1))
                {
                    string[] globProj = _settings.GlobalProjects;
                    for (int i = 0; i < globProj.Length; i++)
                    {
                        if (value.Installation == globProj[i]) value.Installation = "1";
                    }

                    var SQLStringInsert = @"Insert into Amos.A1ATE_ReposPA (Project, ProjectDesc, Account, AccountDesc, Installation, Date_from, Date_to) 
                                            Values (@Project, @ProjectDesc, @Account, @AccountDesc, @Installation, @DateFrom, @DateTo)";
                    try
                    {
                        dbConnectionAmos.Execute(SQLStringInsert, new { value.Project, value.ProjectDesc, value.Account, value.AccountDesc, value.Installation, value.DateFrom, value.DateTo });
                    }
                    catch (Exception ex)
                    {
                        _workerLogger.LogError(ex.ToString());
                    }
                }

                dbConnectionUBW.Close();

                _workerLogger.LogInformation("Executing SP Amos.A1ATE_SP_ReposPA...");
                var SQLStringExec = ("Exec Amos.A1ATE_SP_ReposPA");
                dbConnectionAmos.Execute(SQLStringExec, commandTimeout: 60 * 60);
                _workerLogger.LogInformation("Executing SP Amos.A1ATE_DIST_VH...");
                SQLStringExec = ("Exec Amos.A1ATE_DIST_VH");
                dbConnectionAmos.Execute(SQLStringExec, commandTimeout: 60 * 60);

                var GetValues = @"Select Project, Account from A1ATE_ReposPA Where date_from > GETDATE() or date_to < GETDATE()";
                var SelectProjects = @"Select AccountCodeID from AccountCode where code = @Project"; // PV19075
                var SelectAccounts = @"Select CostCentreID from CostCentre where CostCentreCode = @Account"; // 1294
                var DeleteCCActs = @"Delete from amos.CostCentreAccount where AccountCodeID = @projectid and CostCentreID = @accountid"; // 6000015608 - 6000000011
                foreach (var value in dbConnectionAmos.Query<Aglrelvalue>(GetValues))
                {
                    foreach(var projectid in dbConnectionAmos.Query<long>(SelectProjects, new { Project = value.Project }))
                    {
                        foreach(var accountid in dbConnectionAmos.Query<long>(SelectAccounts, new { Account = value.Account }))
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
            return Task.FromResult(JobResult.Success("OK"));
        }
    }
}