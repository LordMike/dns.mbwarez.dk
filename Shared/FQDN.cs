using System;
using System.Globalization;
using System.Linq;

namespace Shared
{
    public struct FQDN : IEquatable<FQDN>
    {
        public static FQDN Blank = new FQDN(string.Empty);

        private static IdnMapping _mapping = new IdnMapping();

        public string DnsName { get; private set; }

        public FQDN(string domainName)
            : this()
        {
            if (domainName == null)
                throw new ArgumentNullException("domainName");

            domainName = domainName.ToLower().Trim().TrimEnd('.');

            if (domainName.Length == 0)
                DnsName = "";
            else
                DnsName = _mapping.GetAscii(domainName);
        }

        public bool Equals(FQDN other)
        {
            return other != null && other.DnsName.Equals(DnsName);
        }

        public override int GetHashCode()
        {
            return DnsName.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return Equals((FQDN)obj);
        }

        public override string ToString()
        {
            return FullInternationalName;
        }

        public string FullInternationalName { get { return _mapping.GetUnicode(DnsName) + "."; } }
        public string FullDnsName { get { return DnsName + "."; } }

        public FQDN GetParentDomainName()
        {
            string[] labels = GetLabels();
            string parentDomain = string.Join(".", labels.Skip(1));

            return new FQDN(parentDomain);
        }

        public FQDN GetParentDomainName(int level)
        {
            string dnsName = DnsName;
            int levels = 0;

            for (int i = dnsName.Length - 1; i >= 0; i--)
            {
                if (dnsName[i] == '.')
                    levels++;

                if (levels == level)
                    return new FQDN(dnsName.Substring(i + 1, dnsName.Length - i - 1));
            }

            return this;
        }

        public int GetLabelCount()
        {
            int count = 1;
            for (int i = 0; i < DnsName.Length; i++)
                if (DnsName[i] == '.')
                    count++;

            return count;
        }

        public string[] GetLabels()
        {
            return DnsName.Split('.');
        }

        public static bool operator ==(FQDN a, FQDN b)
        {
            return !ReferenceEquals(a, null) && !ReferenceEquals(b, null) && a.Equals(b);
        }

        public static bool operator !=(FQDN a, FQDN b)
        {
            return !(a == b);
        }
    }
}