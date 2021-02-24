using System;
using System.Collections.Generic;
using System.Text;

namespace SER.Models
{
    public class GraphStatusRequest
    {
        public string ClassName { get; set; }
        public int Action { get; set; }
        public string Id { get; set; }
        public string CompanyId { get; set; }
    }
}
