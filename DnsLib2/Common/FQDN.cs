using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace DnsLib2.Common
{
    public struct FQDN : IEquatable<FQDN>
    {
        private static Regex _fqdnRegex = new Regex(@"^(?:\.|(?:(?:[a-z0-9\-_](?:[a-z0-9\-_]{0,61}[a-z0-9\-_])?|\*)\\?\.)+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static IdnMapping _mapping = new IdnMapping();

        public static FQDN Blank = new FQDN(".");

        private readonly int _hashCode;

        public string Name { get; private set; }

        public bool IsValidFqdn { get; private set; }

        public FQDN(string name)
            : this()
        {
            if (!name.EndsWith("."))
                name = name + ".";

            IsValidFqdn = _fqdnRegex.IsMatch(name);
            //if (!_fqdnRegex.IsMatch(name))
            //    throw new ArgumentException("Input was not a valid FQDN", name);

            _hashCode = name.ToLower().GetHashCode();
            Name = name;
        }

        public static FQDN CreateFromUnicode(string name)
        {
            return new FQDN(_mapping.GetAscii(name));
        }

        public bool Equals(FQDN other)
        {
            return string.Equals(Name, other.Name, StringComparison.InvariantCultureIgnoreCase);
        }

        public bool Equals(string other)
        {
            return string.Equals(Name, other, StringComparison.InvariantCultureIgnoreCase);
        }

        public override int GetHashCode()
        {
            return _hashCode;
        }

        public IEnumerable<string> GetLabels()
        {
            return Name.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
        }

        public bool IsSubdomainOf(FQDN parent)
        {
            return Name.EndsWith("." + parent.Name);
        }

        public bool IsParentOf(FQDN child)
        {
            return child.Name.EndsWith("." + Name);
        }

        public static bool operator ==(FQDN a, FQDN b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(FQDN a, FQDN b)
        {
            return !(a == b);
        }

        public override string ToString()
        {
            return Name ?? "<none>";
        }
    }
}
