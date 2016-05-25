using System.Net;
using System.Threading.Tasks;

namespace DnsLib2
{
    public interface IDnsClient
    {
        Task<DnsMessageView> Query(IPEndPoint remoteServer, DnsMessageBuilder builder);
    }
}