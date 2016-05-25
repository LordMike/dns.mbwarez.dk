using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using DnsLib2;
using DnsLib2.Common;
using DnsLib2.Enums;
using DnsLib2.Records;

namespace TldScraper
{
    public static class Utilities
    {
        public static IEnumerable<IPEndPoint> FindNameservers(IPEndPoint recursiveServer, FQDN zone)
        {
            using (UdpDnsClient client = new UdpDnsClient(recursiveServer.AddressFamily))
            {
                List<FQDN> nameservers = null;

                for (int i = 0; i < 3; i++)
                {
                    DnsMessageView res;

                    try
                    {
                        res = client.Query(recursiveServer, zone, QType.NS).Result;
                    }
                    catch (Exception)
                    {
                        continue;
                    }

                    nameservers = res.AllRecords().OfType<RecordViewNS>().Select(s => s.Reference).ToList();
                    break;
                }

                if (nameservers == null)
                    yield break;

                // Find nameservers IP's
                foreach (FQDN nameserver in nameservers)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        DnsMessageView res;

                        try
                        {
                            res = client.Query(recursiveServer, nameserver, QType.A).Result;
                        }
                        catch (Exception)
                        {
                            continue;
                        }

                        foreach (IPAddress address in res.Answers().OfType<RecordViewA>().Select(s => s.IpAddress))
                            yield return new IPEndPoint(address, 53);

                        break;
                    }

                    for (int i = 0; i < 3; i++)
                    {
                        DnsMessageView res;

                        try
                        {
                            res = client.Query(recursiveServer, nameserver, QType.AAAA).Result;
                        }
                        catch (Exception)
                        {
                            continue;
                        }

                        foreach (IPAddress address in res.Answers().OfType<RecordViewAAAA>().Select(s => s.IpAddress))
                            yield return new IPEndPoint(address, 53);

                        break;
                    }
                }
            }
        }
    }
}