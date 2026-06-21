using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fjord1.Int.API.Models.DB
{
    public class OrderLine
    {
        public long OrderId { get; set; }
        public int OrderLineNo { get; set; }
        public string Name { get; set; }
        public double Price { get; set; }
        public double Quantity { get; set; }
        public string Commennt1 { get; set; }
        public string Commennt2 { get; set; }
        public double Discount { get; set; }
        public double DiscAmount { get; set; }
        public string Budsjettkode { get; set; }
        public string Rappgrp { get; set; }
        public string TaxCode { get; set; }
        public int TaxPercent { get; set; }
        public double Cancelled { get; set; }

    }
}
