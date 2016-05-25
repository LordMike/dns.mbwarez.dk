using System.Collections.Generic;
using WebShared.Db;

namespace DnsMbwarezDk.Models.Data
{
    public class DataIndexModel
    {
        public string FilterText { get; set; }

        public bool FilterAxfr { get; set; }

        public bool FilterNsec { get; set; }

        public bool FilterNsec3 { get; set; }

        public bool FilterFtp { get; set; }

        public bool FilterRsync { get; set; }

        public bool FilterIssues { get; set; }

        public bool FilterLevelFirst { get; set; }

        public bool FilterLevelSecond { get; set; }

        public List<TldDomain> States { get; set; }
    }
}