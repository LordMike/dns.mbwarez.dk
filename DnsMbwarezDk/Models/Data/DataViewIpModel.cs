using System.Collections.Generic;
using WebShared.Db;
using IpInfo = WebShared.Db.IpInfo;
using TldServer = WebShared.Db.TldServer;

namespace DnsMbwarezDk.Models.Data
{
    public class DataViewIpModel
    {
        public string ServerIp { get; set; }

        public IpInfo IpInfo { get; set; }

        public List<TldServer> Tlds { get; set; }

        public ServerFeatureSet CombinedFeatureSet { get; set; }
    }
}