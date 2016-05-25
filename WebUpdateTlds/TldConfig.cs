using System.Collections.Generic;
using WebShared.Db;

namespace WebUpdateTlds
{
    public class TldConfig
    {
        public string Tld { get; set; }

        public TldType Type { get; set; }

        public string Wikipage { get; set; }

        public List<string> SecondLevels { get; set; }

        public TldConfig()
        {
            Type = TldType.Unknown;
            SecondLevels = new List<string>();
        }
    }
}