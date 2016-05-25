using DnsLib2.Common;
using DnsLib2.Enums;

namespace DnsLib2.Records
{
    public abstract class BuildRecordBase
    {
        public FQDN Name { get; set; }
        public QType Type { get; set; }
        public QClass Class { get; set; }
        public uint Ttl { get; set; }

        public BuildRecordBase(FQDN name, uint ttl, QType type = QType.A, QClass @class = QClass.IN)
        {
            Name = name;
            Type = type;
            Class = @class;
            Ttl = ttl;
        }

        public int Encode(byte[] buffer, int offset)
        {
            int newOffset = offset;

            // Encode record
            newOffset += DnsEncoder.Encode(Name, buffer, newOffset);
            newOffset += DnsEncoder.Encode((ushort)Type, buffer, newOffset);
            newOffset += DnsEncoder.Encode((ushort)Class, buffer, newOffset);
            newOffset += DnsEncoder.Encode(Ttl, buffer, newOffset);
            newOffset += DnsEncoder.Encode((ushort)GetPayloadLength(), buffer, newOffset);

            // Encode payload
            newOffset += EncodePayload(buffer, newOffset);

            return newOffset - offset;
        }

        protected abstract int EncodePayload(byte[] buffer, int offset);

        public int GetEncodedLength()
        {
            return DnsEncoder.GetEncodedLength(Name) + 10 + GetPayloadLength();
        }

        protected abstract int GetPayloadLength();
    }
}