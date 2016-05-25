using System.Text;

namespace DnsLib2.Records
{
    public class RecordViewTXT : DnsRecordView
    {
        public string Text
        {
            get
            {
                return Encoding.UTF8.GetString(Data, DataOffset, DataLength - DataOffset);
            }
        }

        public RecordViewTXT(byte[] data, int offset)
            : base(data, offset)
        {
        }
    }
}