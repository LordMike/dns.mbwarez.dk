using System;
using DnsLib2.Common;
using DnsLib2.Enums;

namespace DnsLib2.Records
{
    public class BuildOptRecord : BuildRecordBase
    {
        public byte[] DataBytes { get; set; }

        public BuildOptRecord(ushort udpPayloadsize, uint extendedRcode)
            : base(FQDN.Blank, extendedRcode, QType.OPT, (QClass)udpPayloadsize)
        {
            DataBytes = null;
        }

        protected override int EncodePayload(byte[] buffer, int offset)
        {
            if (DataBytes == null)
                return 0;

            Array.Copy(DataBytes, 0, buffer, offset, DataBytes.Length);
            return DataBytes.Length;
        }

        protected override int GetPayloadLength()
        {
            return DataBytes == null ? 0 : DataBytes.Length;
        }
    }
}