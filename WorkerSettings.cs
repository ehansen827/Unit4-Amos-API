using A1AR.SVC.Worker.Lib.Attributes;
using A1AR.SVC.Worker.Lib.Common;

namespace Fjord1.Int.API
{
	public class WorkerSettings
	{
		//[WorkerSettingConnection("MSSQL", "Server=SRFLOUNIT4DB\\UNIT4;Database=AgressoM7;User Id=AgressoM7;Password=Arribatec_2018")]
		//public IDBConnectionFactory UBWDbConnection { get; set; }

		[WorkerSettingConnection("MSSQL2", "Server=SRFLOUNIT4DB\\UNIT4;Database=A1taskengine;User Id=a1taskengine;Password=Yes!")]
		public IDBConnectionFactory ATEDbConnection { get; set; }

        //[WorkerSettingConnection("MSSQL3", "Server=srfloamosdb2019;Database=AmosOffice;User Id=amos;Password=voyager")] ;User Id=amos;Password=voyager
        [WorkerSettingConnection("MSSQL3", "Server=ERIKSTHINKPAD\\ERIKSPC;Database=Amos;TrustServerCertificate=True;Trusted_Connection=True", DBTypes.SqlServer)]
        public IDBConnectionFactory AmosDbConnection { get; set; }

		public string[] ExcludeSupplier { get; set; }
		public string[] ExcludeInstallation { get; set; }
		public string LastUpdated { get; set; } = "2025-05-31";
        public string RootPath { get; set; }
		public string HoldingPath { get; set; }
		public string FraEHF { get; set; }
		public int DelayLG04 { get; set; }
		public int IncludeLG04 { get; set; }
		public string XmlrawPath { get; set; }
		public string XmlPath { get; set; }
        public string UserNameUBW { get; set; } = "haneri";	
        public string PasswordUBW { get; set; } = "Ymmu!726";
        public string BaseUri { get; set; } = "http://srflounit4tsapp/TESTAgressoM7-web-api"; 
        public string SourceAPI { get; set; }
        public string InvoicePath { get; set; }
		public string IncomingPath { get; set; }
		public string ErrorPath { get; set; }
		public string FormNo { get; set; }
		public int OrdLimit { get; set; }
		public string Account { get; set; }
		public string[] GlobalProjects { get; set; }
		public string Stylesheet { get; set; }
		public string StylesheetName { get; set; }
		public string RsPath { get; set; } = @"C:\Slettmeg\";
		public string[] RsClient { get; set; } = { "50","60","70","21","55","71","72","73","74" };
		public string RsFilename { get; set; } = "F1Bestiller.xml";
        public bool FullSync { get; set; }
        public string InvoiceNo { get; set; }
        public int DelayMins { get; set; }
        public string SQLInjection { get; set; }
		public string ApiClient { get; set; }  = "/v1/objects/osgubwclients?filter=client%20eq%20";
		public string ApiBestiller { get; set; } = "/v1/objects/osgrsf1bestillers?companyId=";
		public string ApiSupplier { get; set; } = "/v1/objects/osgrssuppliers?companyId=";
		public string ApiSuppSync { get; set; } = "/v1/objects/suppliers?companyId=50&filter=lastUpdated%2FupdatedAt%20gt%20";
		public string ApiUBWOrder { get; set; } = "/v1/objects/osgorderubws?companyId=50&filter=lastUpdate%20gt%20";
		public string ApiMoveInv { get; set; }  = "/v1/objects/osgorderubws?companyId=50&filter=lastUpdate%20gt%20";
		public string ApiSyncProj { get; set; } = "/v1/objects/osgsyncprojs?companyId=50";
		public string ApiAccounts { get; set; } = "/v1/objects/osgaccountss?companyId=50";
		public string ApiProjects { get; set; } = "/v1/objects/osgprojectss?companyId=50";
        //public string username { get; set; } = "haneri";
        //public string password { get; set; } ="Ymmu!726";
        //public string apiBaseUrl = "https://ubw.unit4cloud.com/no_fj1_prod_webapi"; 

    }
}