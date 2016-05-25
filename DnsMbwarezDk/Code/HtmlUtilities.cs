using System.Globalization;
using System.Linq;

namespace DnsMbwarezDk.Code
{
    public static class HtmlUtilities
    {
        private static IdnMapping _idnMapping = new IdnMapping();

        public static string DomainToIdn(string domain)
        {
            if (domain == ".")
                return ".";

            return _idnMapping.GetUnicode(domain);
        }

        public static string DomainToEmail(string domain)
        {
            if (string.IsNullOrEmpty(domain))
                return null;

            string[] splits = domain.Split('.');
            return splits.First() + "@" + string.Join(".", splits.Skip(1));
        }

        public static string PrepTldForUrl(string domain)
        {
            return domain.TrimEnd('.');
        }
    }
}