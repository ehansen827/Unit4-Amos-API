using A1AR.SVC.Worker.Lib.Common;
using Microsoft.Extensions.Logging;
using System;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Fjord1.Int.API.Utilities;
using Fjord1.Int.API.Models.DB;

namespace Fjord1.Int.API.Workers
{
    public class SupplierSync55 : Worker, IWorkerSettings<WorkerSettings>
    {
        private readonly ILogger<Worker> _workerLogger;
        private readonly WorkerSettings _settings;
        public Type SettingsType => typeof(WorkerSettings);

        public SupplierSync55(ILogger<Worker> workerLogger, WorkerSettings WorkerSettings)
        {
            _workerLogger = workerLogger;
            _settings = WorkerSettings;
        }

        public override async Task<JobResult> Execute()
        {
            try
            {
                using IDbConnection dbConnectionUBW = _settings.UBWDbConnection.CreateConnection();
                dbConnectionUBW.Open();

                var LastSuccessFulRun = _settings.LastSucessfulRun(this.JobInstance.Id).ToString("yyyy-MM-dd HH:mm");
                _workerLogger.LogInformation($"Last updated: {LastSuccessFulRun}");
                if (!string.IsNullOrEmpty(_settings.LastUpdated)) LastSuccessFulRun = _settings.LastUpdated;
                if (_settings.FullSync is true) LastSuccessFulRun = null;
                _workerLogger.LogInformation($"Synchronizing supplier information updated since " + LastSuccessFulRun);

                var SQLSelectSup = @$"SELECT t.apar_id,
                                    t.apar_name,
                                    t.vat_reg_no,
                                    t.comp_reg_no,
                                    a1.address,
									a1.description,
									a1.e_mail,
									a1.telephone_1,
                                    a1.zip_code,
                                    a1.place,
                                    a1.country_code,
                                    t.currency,
                                    t.status
                                    From asuheader t  
                                    Inner join agladdress a1 ON(1=1)
                                    Where 1=1 
                                    AND COALESCE(cast(convert(char(8),t.expired_date,112)AS datetime) , convert(datetime,'19000101',112)) = convert(datetime,'19000101',112) 
                                    AND t.client = '55'
                                    AND t.apar_id = a1.dim_value 
                                    AND a1.attribute_id= 'A5' 
                                    AND a1.address_type = '1'
                                    AND t.last_update >= @LastSuccessFulRun
                                    {_settings.SQLInjection}
                                    ";

                foreach (var value in dbConnectionUBW.Query<SyncSup55>(SQLSelectSup, new { LastSuccessFulRun }))
                {
                    _workerLogger.LogInformation("Processing supplier " + value.apar_name );

                        using IDbConnection dbConnectionAmos = _settings.AmosDbConnection.CreateConnection();
                        dbConnectionAmos.Open();
                    if (string.IsNullOrWhiteSpace(value.currency)) value.currency = "NOK";
               
                    var SQLExec = $"Exec Amos.F1_MoveLEVFromAgressoData55 " +
                        $"'{value.apar_name.Replace("'","")}', {value.apar_id}, '{value.address.Replace("'", "")}', '{value.zip_code}', '{value.place.Replace("'", "")}', " +
                        $"'{value.country_code}', '{value.telephone_1}','{value.e_mail.Replace("'", "")}','{value.status}', " +
                        $"'{value.vat_reg_no}', '{value.com_reg_no}','{value.description.Replace("'", "")}','{value.currency}'"
                        ;
                    _workerLogger.LogInformation(SQLExec);
                    dbConnectionAmos.Execute(SQLExec); 
                    dbConnectionAmos.Close();

                    _workerLogger.LogInformation("done SQLExec");
                }
                //var SQLUpdateSup = @"SELECT apar_id, apar_name, asu.status, amosadr.addressid, amosadr.name, amosadr.gradeid, amosgra.descr,
                //                    CASE
                //                     WHEN amosadr.addressid is null THEN 'Yes' ELSE 'No'
                //                    END AS New, 
                //                    CASE	
                //                     WHEN status = 'N' AND amosgra.gradeid != 1000001 THEN 'Yes' 
                //                     WHEN status = 'P' AND amosgra.gradeid != 1000002 THEN 'Yes' 
                //                     WHEN status = 'C' AND amosgra.gradeid != 1000003 THEN 'Yes' 
                //                     ELSE 'No' 
                //                    END AS Updated 
                //                    FROM [dbo].[asuheader] asu 
                //                    LEFT JOIN [srfloamosdb].[Demo].[amos].[Address] amosadr ON asu.apar_id = amosadr.code
                //                    LEFT JOIN [srfloamosdb].[Demo].[amos].qagrading amosgra ON amosadr.gradeid = amosgra.gradeid
                //                    WHERE client in ('55')
                //                    AND asu.last_update >= @LastSuccessFulRun
                //                    ";


                #region Connection to prod:
                var SQLUpdateSup = @"SELECT apar_id, apar_name, asu.status, amosadr.addressid, amosadr.name, amosadr.gradeid, amosgra.descr,
                                    CASE
                                     WHEN amosadr.addressid is null THEN 'Yes' ELSE 'No'
                                    END AS New, 
                                    CASE	
                                     WHEN status = 'N' AND amosgra.gradeid != 1000001 THEN 'Yes' 
                                     WHEN status = 'P' AND amosgra.gradeid != 1000002 THEN 'Yes' 
                                     WHEN status = 'C' AND amosgra.gradeid != 1000003 THEN 'Yes' 
                                     ELSE 'No' 
                                    END AS Updated 
                                    FROM [dbo].[asuheader] asu 
                                    LEFT JOIN [srfloamosdb2019].[AmosOffice].[amos].[Address] amosadr ON asu.apar_id = amosadr.addressid
                                    LEFT JOIN [srfloamosdb2019].[AmosOffice].[amos].qagrading amosgra ON amosadr.gradeid = amosgra.gradeid
                                    WHERE client = '55' 
                                    AND asu.last_update >= @LastSuccessFulRun
                                    ";
                #endregion

                var SQLUpdateAdr = @"UPDATE Address
                                    SET gradeid = @gradeid
                                    WHERE code = @apar_id and deptid = 6000000001";

                var gradeid = 0;

                foreach (var value in dbConnectionUBW.Query<SyncSup>(SQLUpdateSup, new { LastSuccessFulRun }))
                {
                    _workerLogger.LogInformation("Processing 2 supplier " + value.apar_name);
                    _workerLogger.LogInformation("Updated: " + value.Updated);
                    if (value.Updated == "Yes")
                    {
                        _workerLogger.LogInformation("Updating supplier " + value.apar_name);

                        using IDbConnection dbConnectionAmos = _settings.AmosDbConnection.CreateConnection();
                        dbConnectionAmos.Open();

                        _workerLogger.LogInformation("Updating supplier " + value.apar_name);
                        if (value.status == "N") gradeid = 1000001;
                        if (value.status == "P") gradeid = 1000002;
                        if (value.status == "C") gradeid = 1000003;

                        _workerLogger.LogInformation("start SQLUpdateAdr ");
                        dbConnectionAmos.Execute(SQLUpdateAdr, new { value.apar_id, gradeid });
                        dbConnectionAmos.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                _workerLogger.LogError(ex.ToString());
                return JobResult.Failed("Failed: " + ex.Message);
            }
            return JobResult.Success("OK");
        }
    }
}