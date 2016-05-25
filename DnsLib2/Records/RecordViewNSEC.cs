using System;
using DnsLib2.Common;

namespace DnsLib2.Records
{
    public class RecordViewNSEC : DnsRecordView
    {
        public FQDN NextName { get; private set; }

        public byte[] NsecBits { get; private set; }

        public RecordViewNSEC(byte[] data, int offset)
            : base(data, offset)
        {
            FQDN fqdn;
            int fqdnDataLength = DnsEncoder.DecodeFqdn(Data, DataOffset, out fqdn);
            NextName = fqdn;

            NsecBits = new byte[DataLength - fqdnDataLength];
            Array.Copy(Data, DataOffset + fqdnDataLength, NsecBits, 0, NsecBits.Length);
        }
    }
}