using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DnsLib2.Common;
using WebShared.Db;

namespace WebUpdateTlds
{
    public class TldConfigCollection
    {
        private Dictionary<FQDN, TldConfig> _configs;

        private TldConfigCollection()
        {
            _configs = new Dictionary<FQDN, TldConfig>();
        }

        public int Count { get { return _configs.Count; } }

        public List<TldConfig> All { get { return _configs.Values.ToList(); } }

        public static TldConfigCollection FromFile(string file)
        {
            TldConfigCollection res = new TldConfigCollection();

            if (!File.Exists(file))
                return res;

            TldConfig current = null;
            foreach (string line in File.ReadLines(file))
            {
                if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                    continue;

                if (line.EndsWith(".") && line.IndexOf(' ') == -1)
                {
                    // New TLD
                    FQDN fqdn = new FQDN(line);

                    current = new TldConfig { Tld = fqdn.Name };
                    res._configs[fqdn] = current;

                    continue;
                }

                if (current == null)
                    continue;

                string key = line.Substring(0, line.IndexOf(' '));
                string lineRest = line.Substring(key.Length + 1);

                switch (key)
                {
                    case "type":
                        current.Type = (TldType)Enum.Parse(typeof(TldType), lineRest);
                        break;
                    case "wiki":
                        current.Wikipage = lineRest;
                        break;
                    case "second":
                        current.SecondLevels.AddRange(lineRest.Split(' '));
                        break;
                }
            }

            return res;
        }

        public TldConfig Get(FQDN tld)
        {
            TldConfig tmp;
            _configs.TryGetValue(tld, out tmp);

            return tmp;
        }
    }
}