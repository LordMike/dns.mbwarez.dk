using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebShared.Db
{
    public class TldDomain
    {
        [Key]
        [MaxLength(255)]
        public string Domain { get; set; }

        [MaxLength(255)]
        public string ParentTld { get; set; }

        public int DomainLevel { get; set; }

        public bool IsActive { get; set; }

        [Required]
        public DateTime CreatedTime { get; set; }

        public TldInfo Info { get; set; }

        [MaxLength(255)]
        public string MasterResponsibleName { get; set; }

        [MaxLength(255)]
        public string MasterServerDnsName { get; set; }

        public long SoaRefreshTime { get; set; }

        public virtual ICollection<TldServer> Servers { get; set; }

        public virtual ICollection<DomainScrapeHistory> ScrapeHistory { get; set; }

        public TldDomain()
        {
            Servers = new List<TldServer>();
        }
    }
}