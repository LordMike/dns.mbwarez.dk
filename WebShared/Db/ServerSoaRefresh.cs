using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebShared.Db
{
    [ComplexType]
    public class ServerSoaRefresh
    {
        public DateTime LastCheckUtc { get; set; }

        public bool LastCheckSuccess { get; set; }

        public long Serial { get; set; }

        public int RefreshTime { get; set; }

        [MaxLength(255)]
        public string MasterServerDnsName { get; set; }

        [MaxLength(255)]
        public string MasterResponsibleName { get; set; }

        public ServerSoaRefresh()
        {
            LastCheckUtc = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        }
    }
}