using DnsLib2.Common;
using DnsLib2.Enums;

namespace DnsLib2.Records
{
    public class BuildRecordNS : BuildRecordBase
    {
        public FQDN Reference { get; set; }

        public BuildRecordNS(FQDN name, uint ttl, FQDN reference)
            : base(name, ttl, QType.NS, QClass.IN)
        {
            Reference = reference;
        }

        protected override int EncodePayload(byte[] buffer, int offset)
        {
          return  DnsEncoder.Encode(Reference, buffer, offset);
        }

        protected override int GetPayloadLength()
        {
            return DnsEncoder.GetEncodedLength(Reference);
        }
    }
}