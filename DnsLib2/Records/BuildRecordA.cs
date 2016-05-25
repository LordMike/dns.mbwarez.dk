using System;
using System.Net;
using System.Net.Sockets;
using DnsLib2.Common;
using DnsLib2.Enums;

namespace DnsLib2.Records
{
    public class BuildRecordA : BuildRecordBase
    {
        public IPAddress IpAddress { get; set; }

        public BuildRecordA(FQDN name, uint ttl, IPAddress ipAddress)
            : base(name, ttl, QType.A, QClass.IN)
        {
            if (ipAddress.AddressFamily != AddressFamily.InterNetwork)
                throw new ArgumentException("IP Must be IPv4");

            IpAddress = ipAddress;
        }

        protected override int EncodePayload(byte[] buffer, int offset)
        {
            byte[] ipBytes = IpAddress.GetAddressBytes();
            Array.Copy(ipBytes, 0, buffer, offset, ipBytes.Length);

            return ipBytes.Length;
        }

        protected override int GetPayloadLength()
        {
            return 4;
        }
    }
}