using System;
using DnsLib2.Common;
using Shared;

namespace DnsLib2.Records
{
    public class RecordViewNSEC3 : DnsRecordView
    {
        public Nsec3HashAlgorithm_ HashAlgorithm { get; set; }

        public byte Flags { get; set; }

        public ushort Iterations { get; set; }

        public byte[] Salt { get; set; }

        public byte[] Hash { get; set; }

        public byte[] Bitmap { get; set; }

        public string NextHashedName { get { return Base32.Encode(Hash); } }

        public RecordViewNSEC3(byte[] data, int offset)
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

            length = Data[tmpOffset++];
            Hash = new byte[length];
            Array.Copy(Data, tmpOffset, Hash, 0, Hash.Length);
            tmpOffset += length;

            length = DataLength - (tmpOffset - DataOffset);
            Bitmap = new byte[length];
            Array.Copy(Data, tmpOffset, Bitmap, 0, Bitmap.Length);
            tmpOffset += length;
        }
    }
}