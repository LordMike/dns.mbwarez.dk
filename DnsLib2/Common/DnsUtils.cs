namespace DnsLib2.Common
{
    public static class DnsUtils
    {
        public static uint HostToNetworkOrder(uint host)
        {
            return (((uint)HostToNetworkOrder((ushort)host) & 0xFFFF) << 16)
                   | ((uint)HostToNetworkOrder((ushort)(host >> 16)) & 0xFFFF);
        }

        public static ushort HostToNetworkOrder(ushort host)
        {
            return (ushort)((((uint)host & 0xFF) << 8) | (uint)((host >> 8) & 0xFF));
        }
    }
}