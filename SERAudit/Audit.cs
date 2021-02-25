using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace SER.Models.SERAudit
{
    public enum AudiState
    {
        READ = 0,
        CREATE = 1,
        UPDATE = 2,
        DELETE = 3,
        EXECUTE = 4,
        LOGIN = 5,
        LOGOUT = 6
    }

    public class Audit
    {
        public int id { get; set; }
        public DateTime date { get; set; }

        public AudiState action { get; set; }

        [StringLength(40)]
        public string objeto { get; set; }

        [StringLength(200)]
        public string username { get; set; }

        [StringLength(100)]
        public string role { get; set; }

        [StringLength(2000)]
        public string json_browser { get; set; }

        [StringLength(2000)]
        public string json_request { get; set; }

        [Column(TypeName = "jsonb")]
        public string data { get; set; }

        [StringLength(80)]
        public string user_id { get; set; }
    }

    public class AuditBinding
    {
        public AudiState action { get; set; }
        public string objeto { get; set; }
        public Dictionary<string, string> extras { get; set; }
    }
}
