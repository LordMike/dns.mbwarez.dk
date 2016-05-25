using System;
using System.Net;

namespace DnsLib2.Records
{
    public class RecordViewA : DnsRecordView
    {
        public IPAddress IpAddress
        {
            get
            {
                byte[] tmpData = new byte[4];
                Array.Copy(Data, DataOffset, tmpData, 0, tmpData.Length);
                return new IPAddress(tmpData);
            }
        }

        public RecordViewA(byte[] data, int offset)
            : base(data, offset)
        {
        }
    }
}