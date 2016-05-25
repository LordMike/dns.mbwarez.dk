using System.ComponentModel.DataAnnotations.Schema;

namespace WebShared.Db
{
    [ComplexType]
    public class ServerFeatureSet
    {
        public bool SupportsTcp { get; set; }

        public bool SupportsUdp { get; set; }

        public bool SupportsAxfr { get; set; }

        public bool SupportsNsec { get; set; }

        public bool SupportsNsec3 { get; set; }

        public bool SupportsTcpFtp { get; set; }

        public bool SupportsTcpRsync { get; set; }

        public ServerFeatureSet Combine(ServerFeatureSet other)
        {
            return new ServerFeatureSet
            {
                SupportsTcp = SupportsTcp || other.SupportsTcp,
                SupportsUdp = SupportsUdp || other.SupportsUdp,
                SupportsAxfr = SupportsAxfr || other.SupportsAxfr,
                SupportsNsec = SupportsNsec || other.SupportsNsec,
                SupportsNsec3 = SupportsNsec3 || other.SupportsNsec3,
                SupportsTcpFtp = SupportsTcpFtp || other.SupportsTcpFtp,
                SupportsTcpRsync = SupportsTcpRsync || other.SupportsTcpRsync,
            };
        }
    }
}