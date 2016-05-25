using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebShared.Db
{
    public class DomainScrapeHistory
    {
        [Key]
        public int Id { get; set; }

        [Index]
        [Required]
        [MaxLength(255)]
        public string Fqdn { get; set; }

        public DateTime ScanTimeUtc { get; set; }

        [Required]
        [MaxLength(60)]
        public string ScanType { get; set; }

        [ForeignKey("Fqdn")]
        public virtual TldDomain TldDomain { get; set; }
    }
}