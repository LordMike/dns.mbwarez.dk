using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebShared.Db
{
    public class DomainIp
    {
        [Key, Column(Order = 0)]
        [MaxLength(255)]
        public string Fqdn { get; set; }

        [Key, Column(Order = 1)]
        [MaxLength(64)]
        public string Ip { get; set; }

        public DateTime LastSeenUtc { get; set; }

        [ForeignKey("Fqdn")]
        public virtual Domain Domain { get; set; }
    }
}