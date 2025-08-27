using A1AR.SVC.Worker.Lib.Common;
using System;
using System.Collections.Generic;
using System.Text;
using Dapper;
using System.Data.SqlClient;
using System.Data;
using Fjord1.Int.API;

namespace Fjord1.Int.API.Utilities
{
    public static class StringHelpers 
    {
        public static string SafeSubstring(this string orig, int startIndex, int? length = null)
        {
            if (startIndex < 0 || startIndex >= orig.Length)
                return string.Empty;
            if (length == null)
                length = orig.Length - startIndex;
            int len = length ?? 0;
            return orig.Substring(startIndex, orig.Length >= len + startIndex ? len : orig.Length - startIndex);
        }
        public static DateTime LastSucessfulRun(this WorkerSettings settings, Guid taskId)
        {
            using IDbConnection dbConnectionATE = settings.ATEDbConnection.CreateConnection();
            dbConnectionATE.Open();
            var SQLStringGetRun = @"SELECT MAX(ti.ExecutionFinish)
                                    FROM [A1TASKENGINE].[ATE].[TaskInstances] ti
                                    INNER JOIN [A1TASKENGINE].[ATE].[TaskInstances] td ON td.TaskDefinitionId = ti.TaskDefinitionId 
                                    WHERE ti.result = 1 AND td.Id = @taskId";
            var res = dbConnectionATE.QuerySingle<DateTime?>(SQLStringGetRun, new { taskId });
            if (string.IsNullOrEmpty(res.ToString())) res = DateTime.Now;
            return Convert.ToDateTime(res);
        }
        public static bool IsNumeric(this string input)
        {
            return long.TryParse(input, out _);
        }

        public static bool IsBase64(this string base64String)
        {
            if (string.IsNullOrEmpty(base64String) || base64String.Length % 4 != 0 || base64String.Contains(" ") || base64String.Contains("\t")) // || base64String.Contains("\r") || base64String.Contains("\n"))
                return false;
            else return true;
        }
    }
}
