using System;
using System.Collections.Generic;
using System.Text;

namespace Fjord1.Int.API.Models.DB  
{
    public class SyncSup
    {
        public int SupplierId { get; set; }
        public string SupplierName { get; set; }
        public string Status { get; set; }
        public LastUpdated lastUpdated { get; set; }
    }

    public class LastUpdated
    {
        public DateTime updatedAt { get; set; }
        public string updatedBy { get; set; }
    }
    //public class SyncSup
    //{
    //    public int apar_id { get; set; }
    //    public string apar_name { get; set; }
    //    public string status { get; set; }
    //    public long addressid { get; set; }
    //    public string name { get; set; }
    //    public long gradeid { get; set; }
    //    public string descr { get; set; }
    //    public string New { get; set; }
    //    public string Updated { get; set; }
    //}
}
