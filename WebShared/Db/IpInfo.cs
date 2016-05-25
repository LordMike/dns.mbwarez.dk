using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace WebShared.Db
{
    public class IpInfo
    {
        [Key]
        [MaxLength(64)]
        public string Ip { get; set; }

        [Required]
        public DateTime LastUpdateUtc { get; set; }

        [MaxLength(255)]
        public string Hostname { get; set; }

        [MaxLength(255)]
        public string Loc { get; set; }

        [MaxLength(255)]
        public string Org { get; set; }

        [MaxLength(255)]
        public string City { get; set; }

        [MaxLength(255)]
        public string Region { get; set; }

        [MaxLength(255)]
        public string Country { get; set; }

        [MaxLength(255)]
        public string Postal { get; set; }

        public int Phone { get; set; }

        public string ASN { get { return (Org ?? string.Empty).Split(' ').FirstOrDefault(); } }
    }
}