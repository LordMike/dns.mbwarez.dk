using System;
using System.Net;

namespace DnsLib2.Records
{
    public class RecordViewAAAA : DnsRecordView
    {
        public IPAddress IpAddress
        {
            get
            {
                byte[] tmpData = new byte[16];
                Array.Copy(Data, DataOffset, tmpData, 0, tmpData.Length);
                return new IPAddress(tmpData);
            }
        }

        public RecordViewAAAA(byte[] data, int offset)
            : base(data, offset)
        {
        }
    }
}