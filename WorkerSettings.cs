using A1AR.SVC.Worker.Lib.Attributes;
using A1AR.SVC.Worker.Lib.Common;

namespace Fjord1.Int.NetCore
{
	public class WorkerSettings
	{
		[WorkerSettingConnection("MSSQL", "Server=SRFLOUNIT4DB\\UNIT4;Database=AgressoM7;User Id=AgressoM7;Password=Arribatec_2018")]
		public IDBConnectionFactory UBWDbConnection { get; set; }

		[WorkerSettingConnection("MSSQL2", "Server=SRFLOUNIT4DB\\UNIT4;Database=A1taskengine;User Id=a1taskengine;Password=Yes!")]
		public IDBConnectionFactory ATEDbConnection { get; set; }

		[WorkerSettingConnection("MSSQL3", "Server=srfloamosdb2019;Database=AmosOffice;User Id=amos;Password=voyager")]
		public IDBConnectionFactory AmosDbConnection { get; set; }

		public string[] ExcludeSupplier { get; set; }
		public string[] ExcludeInstallation { get; set; }
		public string LastUpdated { get; set; }
		public string RootPath { get; set; }
		public string HoldingPath { get; set; }
		public string FraEHF { get; set; }
		public int DelayLG04 { get; set; }
		public int IncludeLG04 { get; set; }
		public string XmlrawPath { get; set; }
		public string XmlPath { get; set; }
		public string DatanovaPath { get; set; }
		public string InvoicePath { get; set; }
		public string IncomingPath { get; set; }
		public string ErrorPath { get; set; }
		public string FormNo { get; set; }
		public int OrdLimit { get; set; }
		public string Account { get; set; }
		public string[] GlobalProjects { get; set; }
		public string Stylesheet { get; set; }
		public string StylesheetName { get; set; }
		public string RsPath { get; set; }
		public string[] RsClient { get; set; }
        public string RsFilename { get; set; }
        public bool FullSync { get; set; }
        public string InvoiceNo { get; set; }
        public int DelayMins { get; set; }
        public string SQLInjection { get; set; }
    }
}