using A1AR.SVC.Worker.Lib.Common;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using System.Linq;

/*************************************
 * 
 *  Worker designed to update projects  
 *  with current order status from UBW
 * 
 *  Runs every day
 * 
 *************************************/

namespace Fjord1.Int.API.Workers
{
    public class SyncProjects : Worker, IWorkerSettings<WorkerSettings>
    {
        private readonly ILogger<Worker> _workerLogger;
        private readonly WorkerSettings _settings;
        public Type SettingsType => typeof(WorkerSettings);

        public SyncProjects(ILogger<Worker> workerLogger, WorkerSettings WorkerSettings)
        {
            _workerLogger = workerLogger;
            _settings = WorkerSettings;
        }

        public override async Task<JobResult> Execute()
        {
            using IDbConnection dbConnectionAmos = _settings.AmosDbConnection.CreateConnection();
            dbConnectionAmos.Open();

            try
            {
                // Test
                // "SQLInjection":"[SRFLOUNIT4DB\\UNIT4].[TESTAgressoM7].[dbo]"
                // Production
                // "SQLInjection":"[SRFLOUNIT4DB\\UNIT4].[AgressoM7].[dbo]"
                var SQLUpddate1 = $@"UPDATE ac
                                    SET active = 0
                                    FROM AccountCode ac
                                    JOIN {_settings.SQLInjection}.[agldimvalue] agldv ON ac.code = agldv.dim_value
                                    JOIN {_settings.SQLInjection}.[aglrelvalue] aglrv1 ON agldv.dim_value = aglrv1.att_value AND aglrv1.rel_attr_id = 'A0' AND agldv.client = aglrv1.client
                                    JOIN {_settings.SQLInjection}.[aglrelvalue] aglrv2 on agldv.dim_value = aglrv2.att_value AND aglrv2.rel_attr_id = 'C1' AND agldv.client = aglrv2.client
                                    JOIN {_settings.SQLInjection}.[aglrelvalue] aglrv3 ON agldv.dim_value = aglrv3.att_value AND aglrv3.rel_attr_id = 'Z11' AND agldv.client = aglrv3.client
                                    JOIN {_settings.SQLInjection}.[agldescription] agld1 ON agldv.dim_value = agld1.dim_value AND agld1.attribute_id = aglrv1.attribute_id AND agldv.client = agld1.client
                                    JOIN {_settings.SQLInjection}.[agldescription] agld2 ON aglrv1.rel_value = agld2.dim_value AND agld2.attribute_id = 'A0'AND agldv.client = agld2.client
                                    JOIN {_settings.SQLInjection}.[atsproject] pr ON agldv.dim_value = pr.head_project
                                    WHERE 1=1
                                    AND aglrv3.rel_value = 'J'
                                    AND (pr.date_from > GETDATE() OR pr.date_to < GETDATE() OR agldv.status != 'N')";
                var res1 = dbConnectionAmos.Execute(SQLUpddate1, commandTimeout: 60 * 60);
                _workerLogger.LogInformation("Projects disabled in Amos: " + res1);

            }
            catch (Exception ex)
            {
                return JobResult.Failed("Update failed: " + ex);
            }

            return JobResult.Success("OK");
        }
    }
}