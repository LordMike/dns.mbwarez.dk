using DnsLib2.Common;
using DnsLib2.Enums;

namespace DnsLib2.Records
{
    public class BuildRecordCNAME : BuildRecordBase
    {
        public FQDN Reference { get; set; }

        public BuildRecordCNAME(FQDN name, uint ttl, FQDN reference)
            : base(name, ttl, QType.CNAME, QClass.IN)
        {
            Reference = reference;
        }

        protected override int EncodePayload(byte[] buffer, int offset)
        {
            return DnsEncoder.Encode(Reference, buffer, offset);
        }

        protected override int GetPayloadLength()
        {
            return DnsEncoder.GetEncodedLength(Reference);
        }
    }
}