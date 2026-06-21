using System;
using System.Collections.Generic;
using System.Text;

namespace Fjord1.Int.API.Models.DB
{
    public class Projects
    {
        public string Account { get; set; }
        public string AccountDesc { get; set; }
        public string Client { get; set; }
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
        public string Installation { get; set; }
        public string Project { get; set; }
        public string ProjectDesc { get; set; }
    }
}
