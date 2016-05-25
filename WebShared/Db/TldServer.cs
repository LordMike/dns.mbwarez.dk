using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebShared.Db
{
    public class TldServer
    {
        [MaxLength(255)]
        [Key, Column(Order = 0)]
        public string Domain { get; set; }

        [MaxLength(64)]
        [Key, Column(Order = 1)]
        public string ServerIp { get; set; }

        [Required]
        public DateTime CreatedTime { get; set; }

        [MaxLength(255)]
        [Required]
        public string ServerName { get; set; }

        public ServerSoaRefresh Refresh { get; set; }

        public ServerTest Test { get; set; }

        public ServerType ServerType { get; set; }

        [ForeignKey("Domain")]
        public virtual TldDomain DomainItem { get; set; }
    }
}