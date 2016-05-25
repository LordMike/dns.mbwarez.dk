using System;
using DnsLib2.Common;

namespace DnsLib2.Records
{
    public class RecordViewNSEC3PARAM : DnsRecordView
    {
        public Nsec3HashAlgorithm_ HashAlgorithm { get; set; }

        public byte Flags { get; set; }

        public ushort Iterations { get; set; }

        public byte[] Salt { get; set; }

        public RecordViewNSEC3PARAM(byte[] data, int offset)
            : base(data, offset)
        {
            int tmpOffset = DataOffset;

            HashAlgorithm = (Nsec3HashAlgorithm_)Data[tmpOffset++];
            Flags = Data[tmpOffset++];
            Iterations = DnsEncoder.DecodeUshort(Data, tmpOffset);
            tmpOffset += 2;

            int length = Data[tmpOffset++];
            Salt = new byte[length];
            Array.Copy(Data, tmpOffset, Salt, 0, Salt.Length);
            tmpOffset += length;
        }
    }
}