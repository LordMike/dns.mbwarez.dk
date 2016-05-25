using DnsLib2.Common;

namespace DnsLib2.Records
{
    public class RecordViewMX : DnsRecordView
    {
        public FQDN MxServer
        {
            get
            {
                FQDN fqdn;
                DnsEncoder.DecodeFqdn(Data, DataOffset + 2, out fqdn);
                return fqdn;
            }
        }

        public ushort Priority
        {
            get { return DnsEncoder.DecodeUshort(Data, DataOffset); }
        }

        public RecordViewMX(byte[] data, int offset)
            : base(data, offset)
        {
        }
    }
}