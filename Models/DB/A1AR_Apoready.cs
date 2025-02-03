using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fjord1.Int.NetCore
{
    public partial class A1ar_apoready
    {
        public string Accountable { get { return _accountable; } set { _accountable = value.Trim(); } }
        private string _accountable;
        public string Order_id { get; set; }
        public double Estimated_amount { get; set; }
        public DateTime DueDate { get; set; }
        public string Bank_account { get; set; }
        public string Vat_reg_no { get; set; }
        public string  invoice_no { get; set; }
    }
}
