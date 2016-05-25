using System.Text;
using DnsLib2.Common;
using DnsLib2.Enums;

namespace DnsLib2.Records
{
    public class BuildRecordTXT : BuildRecordBase
    {
        public string Text { get; set; }

        public BuildRecordTXT(FQDN name, uint ttl, string text)
            : base(name, ttl, QType.TXT, QClass.IN)
        {
            Text = text;
        }

        protected override int EncodePayload(byte[] buffer, int offset)
        {
            return Encoding.ASCII.GetBytes(Text, 0, Text.Length, buffer, offset);
        }

        protected override int GetPayloadLength()
        {
            return Encoding.ASCII.GetByteCount(Text);
        }
    }
}