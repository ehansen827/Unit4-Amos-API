using System;
using System.Collections.Generic;
using System.Text;

namespace Fjord1.Int.API.Workers
{
    public class WorkerParameters
    {
        public bool Initialize { get; set; }
    }

    public class WorkerParametersPeriod
    {
        public bool Initialize { get; set; }
        public int Period { get; set; }
    }
    public class WorkerParametersDate
    {
        public bool Initialize { get; set; }
        public DateTime Date { get; set; }
    }
    public class WorkerParametersPerDate
    {
        public bool Initialize { get; set; }
        public int Period { get; set; }
        public DateTime Date { get; set; }
    }
}