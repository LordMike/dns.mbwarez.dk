using DnsLib2.Common;
using DnsLib2.Enums;

namespace DnsLib2.Records
{
    public abstract class DnsRecordView
    {
        protected byte[] Data;
        protected int Offset; 

        public FQDN QName { get; private set; }
        public QType QType { get; private set; }
        public QClass QClass { get; private set; }
        public uint Ttl { get; private set; }

        protected ushort DataLength { get; private set; }
        protected ushort DataOffset { get; private set; }

        protected DnsRecordView(byte[] data, int offset)
        {
            Data = data;
            Offset = offset;

            // Parse this item directly, as it is small
            FQDN qName;
            int readBytes = DnsEncoder.DecodeFqdn(data, offset, out qName);
            QName = qName;

            QType = (QType)DnsEncoder.DecodeUshort(data, offset + readBytes);
            QClass = (QClass)DnsEncoder.DecodeUshort(data, offset + readBytes + 2);
            Ttl = DnsEncoder.DecodeUint(data, offset + readBytes + 4);

            DataLength = DnsEncoder.DecodeUshort(data, offset + readBytes + 8);
            DataOffset = (ushort)(offset + readBytes + 10);
        }

        public static int GetRecordLength(byte[] data, int offset)
        {
            int fqdnLength = DnsEncoder.GetFqdnLength(data, offset);
            return fqdnLength + 10 + DnsEncoder.DecodeUshort(data, offset + fqdnLength + 8);
        }

        public static QType GetRecordType(byte[] data, int offset)
        {
            int fqdnLength = DnsEncoder.GetFqdnLength(data, offset);
            return (QType)DnsEncoder.DecodeUshort(data, fqdnLength + offset);
        }

        public static DnsRecordView CreateRecord(byte[] data, int offset)
        {
            QType type = GetRecordType(data, offset);

            switch (type)
            {
                case QType.A:
                    return new RecordViewA(data, offset);
                case QType.AAAA:
                    return new RecordViewAAAA(data, offset);
                case QType.CNAME:
                    return new RecordViewCNAME(data, offset);
                case QType.MX:
                    return new RecordViewMX(data, offset);
                case QType.TXT:
                    return new RecordViewTXT(data, offset);
                case QType.NS:
                    return new RecordViewNS(data, offset);
                case QType.SOA:
                    return new RecordViewSOA(data, offset);
                case QType.NSEC:
                    return new RecordViewNSEC(data, offset);
                case QType.NSEC3:
                    return new RecordViewNSEC3(data, offset);
                case QType.NSEC3PARAM:
                    return new RecordViewNSEC3PARAM(data, offset);
                default:
                    return new RecordViewDefault(data, offset);
            }
        }
    }
}