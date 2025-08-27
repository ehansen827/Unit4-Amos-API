using A1AR.SVC.Worker.Lib.Common;
using Microsoft.Data.SqlClient;
using System.Data;

namespace CLI
{
    public class ConnectionFactory : IDBConnectionFactory
    {
        private readonly string connectString;

        public ConnectionFactory(string connectString)
        {
            this.connectString = connectString;
        }

        public IDbConnection CreateConnection()
        {
            return new SqlConnection(connectString);
        }
    }
}