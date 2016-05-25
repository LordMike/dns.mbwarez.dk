using DnsLib2.Common;

namespace DnsLib2.Records
{
    public class RecordViewSOA : DnsRecordView
    {
        public RecordViewSOA(byte[] data, int offset)
            : base(data, offset)
        {
            FQDN fqdn;
            int newOffset = DnsEncoder.DecodeFqdn(Data, DataOffset, out fqdn);
            MasterName = fqdn;

            newOffset += DnsEncoder.DecodeFqdn(Data, DataOffset + newOffset, out fqdn);
            ResponsibleName = fqdn;

            Serial = DnsEncoder.DecodeUint(Data, DataOffset + newOffset);
            Refresh = DnsEncoder.DecodeUint(Data, DataOffset + newOffset + 4);
            Retry = DnsEncoder.DecodeUint(Data, DataOffset + newOffset + 8);
            Expire = DnsEncoder.DecodeUint(Data, DataOffset + newOffset + 12);
            Minimum = DnsEncoder.DecodeUint(Data, DataOffset + newOffset + 16);
        }

        public FQDN MasterName { get; private set; }
        public FQDN ResponsibleName { get; private set; }

        public uint Serial { get; private set; }
        public uint Refresh { get; private set; }
        public uint Retry { get; private set; }
        public uint Expire { get; private set; }
        public uint Minimum { get; private set; }
    }
}