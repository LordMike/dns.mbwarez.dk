using System.Net;
using System.Threading.Tasks;
using DnsLib2.Common;
using DnsLib2.Enums;

namespace DnsLib2
{
    public static class DnsClientExtensions
    {
        public static Task<DnsMessageView> Query(this IDnsClient client, IPAddress server, int port, DnsMessageBuilder builder)
        {
            return client.Query(new IPEndPoint(server, port), builder);
        }

        public static Task<DnsMessageView> Query(this IDnsClient client, IPEndPoint server, FQDN domain, QType type = QType.A, QClass @class = QClass.IN, DnsFlags flags = DnsFlags.RecursionDesired)
        {
            // TODO: Object pool
            DnsMessageBuilder builder = new DnsMessageBuilder()
                .SetQuestion(domain, type, @class)
                .SetFlag(flags)
                .SetOpCode(DnsOpcode.Query);

            return client.Query(server, builder);
        }
    }
}