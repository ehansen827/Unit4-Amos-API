using System;
using System.Collections.Generic;
using System.Text;

namespace Fjord1.Int.API.Models.DB
{
    public class AttributeValue
    {
        public string attributeId { get; set; }
        public string attributeName { get; set; }
        public string attributeValue { get; set; }
        public string companyId { get; set; }
        public double customValue { get; set; }
        public string description { get; set; }
        public string owner { get; set; }
        public string ownerAttributeId { get; set; }
        public string ownerAttributeName { get; set; }
        public int periodFrom { get; set; }
        public int periodTo { get; set; }
        public string status { get; set; }
        //public LastUpdated lastUpdated { get; set; }
    }
}
