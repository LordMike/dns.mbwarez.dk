using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Xml.Serialization;

namespace Shared
{
    public class Zone
    {
        [XmlAttribute]
        public string Name { get; set; }

        [XmlAttribute]
        public DateTime LastSeen { get; set; }

        [XmlAttribute]
        public string LastNameservers { get; set; }

        [XmlAttribute]
        public string LastIps { get; set; }

        [XmlIgnore]
        public IEnumerable<string> LastNameserversParsed
        {
            get { return LastNameservers.Split(';').ToArray(); }
            set { LastNameservers = string.Join(";", value); }
        }

        [XmlIgnore]
        public IEnumerable<IPAddress> LastIpsParsed
        {
            get { return LastIps.Split(';').Select(IPAddress.Parse).ToArray(); }
            set { LastIps = string.Join(";", value.Select(s => s.ToString())); }
        }

        public override string ToString()
        {
            return Name + " (" + LastSeen + ")";
        }
    }
}