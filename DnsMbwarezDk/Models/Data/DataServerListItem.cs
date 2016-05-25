using WebShared.Db;

namespace DnsMbwarezDk.Models.Data
{
    public class DataServerListItem
    {
        public string Ip { get; set; }

        public string Name { get; set; }

        public int DomainCount { get; set; }

        public bool SupportsIpv4 { get; set; }

        public bool SupportsIpv6 { get; set; }

        public ServerFeatureSet FeatureSet { get; set; }
    }
}