using DnsLib2.Common;

namespace DnsLib2.Records
{
    public class RecordViewCNAME : DnsRecordView
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

        public RecordViewCNAME(byte[] data, int offset)
            : base(data, offset)
        {
        }
    }
}