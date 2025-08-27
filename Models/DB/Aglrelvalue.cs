using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fjord1.Int.API.Models.DB
{
    public class Aglrelvalue
    {
        public string Project { get; set; }
        public string AccountDesc { get; set; }
        public string ProjectDesc { get; set; }
        public string Account { get; set; }
        public string Installation { get; set; }
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
    }
}
