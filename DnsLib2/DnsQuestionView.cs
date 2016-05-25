using DnsLib2.Common;
using DnsLib2.Enums;

namespace DnsLib2
{
    public class DnsQuestionView
    {
        public FQDN QName { get; private set; }
        public QType QType { get; private set; }
        public QClass QClass { get; private set; }

        public DnsQuestionView(byte[] data, int offset)
        {
            // Parse this item directly, as it is small
            FQDN qName;
            int readBytes = DnsEncoder.DecodeFqdn(data, offset, out qName);
            QName = qName;

            QType = (QType)DnsEncoder.DecodeUshort(data, offset + readBytes);
            QClass = (QClass)DnsEncoder.DecodeUshort(data, offset + readBytes + 2);
        }

        public static int GetRecordLength(byte[] data, int offset)
        {
            return DnsEncoder.GetFqdnLength(data, offset) + 4;
        }
    }
}