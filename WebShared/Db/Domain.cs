using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebShared.Db
{
    public class Domain
    {
        [Key]
        [MaxLength(255)]
        public string Fqdn { get; set; }

        [MaxLength(255)]
        [Required]
        public string ParentFqdn { get; set; }

        [MaxLength(1024)]
        public string LastNameservers { get; set; }

        public DateTime LastSeenUtc { get; set; }

        public virtual ICollection<DomainIp> Ips { get; set; }
    }
}