using DnsLib2.Common;
using DnsLib2.Enums;

namespace DnsLib2.Records
{
    public class BuildRecordSOA : BuildRecordBase
    {
        public BuildRecordSOA(FQDN name, uint ttl, FQDN masterName, FQDN responsibleName, uint serial, uint retry, uint refresh, uint expire, uint minimum)
            : base(name, ttl, QType.SOA, QClass.IN)
        {
            MasterName = masterName;
            ResponsibleName = responsibleName;
            Serial = serial;
            Retry = retry;
            Refresh = refresh;
            Expire = expire;
            Minimum = minimum;
        }

        public BuildRecordSOA(FQDN name, uint ttl)
            : base(name, ttl, QType.SOA, QClass.IN)
        {
            MasterName = new FQDN(".");
            ResponsibleName = new FQDN(".");
        }

        public FQDN MasterName { get; set; }
        public FQDN ResponsibleName { get; set; }

        public uint Serial { get; set; }
        public uint Refresh { get; set; }
        public uint Retry { get; set; }
        public uint Expire { get; set; }
        public uint Minimum { get; set; }

        protected override int EncodePayload(byte[] buffer, int offset)
        {
            int newOffset = offset;

            newOffset += DnsEncoder.Encode(MasterName, buffer, newOffset);
            newOffset += DnsEncoder.Encode(ResponsibleName, buffer, newOffset);

            newOffset += DnsEncoder.Encode(Serial, buffer, newOffset);
            newOffset += DnsEncoder.Encode(Refresh, buffer, newOffset);
            newOffset += DnsEncoder.Encode(Retry, buffer, newOffset);
            newOffset += DnsEncoder.Encode(Expire, buffer, newOffset);
            newOffset += DnsEncoder.Encode(Minimum, buffer, newOffset);

            return newOffset - offset;
        }

        protected override int GetPayloadLength()
        {
            return DnsEncoder.GetEncodedLength(MasterName) + DnsEncoder.GetEncodedLength(ResponsibleName) + 20;
        }
    }
}