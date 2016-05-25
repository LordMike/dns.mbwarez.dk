using DnsLib2.Common;

namespace DnsLib2.Records
{
    public class RecordViewNS : DnsRecordView
    {
        public FQDN Reference
        {
            get
            {
                FQDN fqdn;
                DnsEncoder.DecodeFqdn(Data, DataOffset, out fqdn);
                return fqdn;
            }
        }

        public RecordViewNS(byte[] data, int offset)
            : base(data, offset)
        {
        }
    }
}