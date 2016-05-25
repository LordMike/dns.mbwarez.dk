using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebShared.Db
{
    [ComplexType]
    public class TldInfo
    {
        public DateTime LastUpdateUtc { get; set; }

        [MaxLength(255)]
        public string Wikipage { get; set; }

        public TldType Type { get; set; }

        public TldInfo()
        {
            LastUpdateUtc = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        }
    }
}