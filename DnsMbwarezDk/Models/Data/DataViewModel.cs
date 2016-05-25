using System.Collections.Generic;
using WebShared.Db;
using IpInfo = WebShared.Db.IpInfo;

namespace DnsMbwarezDk.Models.Data
{
    public class DataViewModel
    {
        public TldDomain Domain { get; set; }

        public ServerFeatureSet CombinedFeatures { get; set; }

        public List<string> ChildTlds { get; set; }

        public Dictionary<string,IpInfo> IpInfos { get; set; }
    }
}