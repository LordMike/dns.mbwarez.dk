using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using DnsLib2;
using DnsLib2.Common;
using DnsLib2.Enums;
using DnsLib2.Records;

namespace WebShared.Utilities
{
    public class NameserverCache
    {
        private ConcurrentDictionary<FQDN, Tuple<FQDN[], DateTime>> _nameservers;
        private ConcurrentDictionary<FQDN, Tuple<IPAddress[], DateTime>> _nameserverIps;

        public IPEndPoint RecursiveServer { get; private set; }

        private UdpDnsClient _client;

        public NameserverCache(IPEndPoint recursiveServer)
        {
            RecursiveServer = recursiveServer;
            _client = new UdpDnsClient(recursiveServer.AddressFamily);

            _nameservers = new ConcurrentDictionary<FQDN, Tuple<FQDN[], DateTime>>();
            _nameserverIps = new ConcurrentDictionary<FQDN, Tuple<IPAddress[], DateTime>>();
        }

        public IEnumerable<FQDN> GetAllDomains()
        {
            return _nameservers.Keys;
        }

        public void RecordNameserver(FQDN domain, FQDN nameserver)
        {
            _nameservers.AddOrUpdate(domain,
                fqdn => new Tuple<FQDN[], DateTime>(new[] { nameserver }, DateTime.UtcNow),
                (fqdn, previous) => new Tuple<FQDN[], DateTime>(previous.Item1.Concat(new[] { nameserver }).ToArray(), DateTime.UtcNow));
        }

        public void RecordNameserverBlank(FQDN domain)
        {
            _nameservers.TryAdd(domain, new Tuple<FQDN[], DateTime>(new FQDN[0], DateTime.UtcNow));
        }

        public void RecordNameserverIp(FQDN nameserver, IPAddress ip)
        {
            _nameserverIps.AddOrUpdate(nameserver,
                fqdn => new Tuple<IPAddress[], DateTime>(new[] { ip }, DateTime.UtcNow),
                (fqdn, previous) => new Tuple<IPAddress[], DateTime>(previous.Item1.Concat(new[] { ip }).ToArray(), DateTime.UtcNow));
        }

        public void RecordNameserverIpBlank(FQDN nameserver)
        {
            _nameserverIps.TryAdd(nameserver, new Tuple<IPAddress[], DateTime>(new IPAddress[0], DateTime.UtcNow));
        }

        public void RecordDnsAnswer(DnsRecordView record)
        {
            RecordViewNS asNs = record as RecordViewNS;
            if (asNs != null)
            {
                RecordNameserver(asNs.QName, asNs.Reference);
                return;
            }

            RecordViewA asA = record as RecordViewA;
            if (asA != null)
            {
                RecordNameserverIp(asA.QName, asA.IpAddress);
                return;
            }

            RecordViewAAAA asAAAA = record as RecordViewAAAA;
            if (asAAAA != null)
            {
                RecordNameserverIp(asAAAA.QName, asAAAA.IpAddress);
                return;
            }
        }

        public void RecordDnsAnswer(DnsMessageView message)
        {
            for (int j = 0; j < message.Header.AnswerCount; j++)
                RecordDnsAnswer(message.GetAnswer(j));

            for (int j = 0; j < message.Header.AuthorityCount; j++)
                RecordDnsAnswer(message.GetAuthority(j));

            for (int j = 0; j < message.Header.AdditionalCount; j++)
                RecordDnsAnswer(message.GetAdditional(j));
        }

        public IEnumerable<FQDN> GetDomainNameservers(FQDN domain)
        {
            if (!_nameservers.ContainsKey(domain))
            {
                DnsMessageBuilder builder = new DnsMessageBuilder().SetFlag(DnsFlags.RecursionDesired).SetQuestion(domain, QType.NS).SetOpCode(DnsOpcode.Query);

                bool success = false;
                for (int i = 0; i < 3; i++)
                {
                    Task<DnsMessageView> resp = _client.Query(RecursiveServer, builder);

                    try
                    {
                        DnsMessageView res = resp.Result;
                        RecordDnsAnswer(res);

                        if (res.Header.ResponseCode == ResponseCode.NoError)
                            success = true;

                        break;
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                }

                if (!success)
                    RecordNameserverBlank(domain);
            }

            Tuple<FQDN[], DateTime> servers;
            if (!_nameservers.TryGetValue(domain, out servers))
                yield break;

            foreach (FQDN server in servers.Item1)
                yield return server;
        }

        public IEnumerable<IPAddress> GetDomainNameserverIps(FQDN domain)
        {
            // Get ips
            return GetDomainNameservers(domain).SelectMany(GetNameserverIps);
        }

        public IEnumerable<IPAddress> GetNameserverIps(FQDN nameserver)
        {
            // Get ips
            if (!_nameserverIps.ContainsKey(nameserver))
            {
                DnsMessageBuilder builder = new DnsMessageBuilder()
                            .SetFlag(DnsFlags.RecursionDesired)
                            .SetQuestion(nameserver, QType.A)
                            .SetOpCode(DnsOpcode.Query);

                bool success = false;

                for (int i = 0; i < 3; i++)
                {
                    Task<DnsMessageView> resp = _client.Query(RecursiveServer, builder);

                    try
                    {
                        RecordDnsAnswer(resp.Result);

                        if (resp.Result.Header.ResponseCode == ResponseCode.NoError)
                            success = true;

                        break;
                    }
                    catch (Exception)
                    {
                        // Retry
                        continue;
                    }
                }

                builder.SetQuestion(nameserver, QType.AAAA);

                for (int i = 0; i < 3; i++)
                {
                    Task<DnsMessageView> resp = _client.Query(RecursiveServer, builder);

                    try
                    {
                        RecordDnsAnswer(resp.Result);

                        if (resp.Result.Header.ResponseCode == ResponseCode.NoError)
                            success = true;

                        break;
                    }
                    catch (Exception)
                    {
                        // Retry
                        continue;
                    }
                }

                if (!success)
                    RecordNameserverIpBlank(nameserver);
            }

            Tuple<IPAddress[], DateTime> ips;
            if (!_nameserverIps.TryGetValue(nameserver, out ips))
                yield break;

            foreach (IPAddress ip in ips.Item1)
                yield return ip;
        }

        public void Save(string file)
        {
            using (StreamWriter sw = new StreamWriter(file))
            {
                foreach (KeyValuePair<FQDN, Tuple<FQDN[], DateTime>> pair in _nameservers)
                    sw.WriteLine("NS " + pair.Key.Name + " " + DateHelper.ToString(pair.Value.Item2) + " " + string.Join(" ", pair.Value.Item1.Select(s => s.Name)));

                foreach (KeyValuePair<FQDN, Tuple<IPAddress[], DateTime>> pair in _nameserverIps)
                    sw.WriteLine("IP " + pair.Key.Name + " " + DateHelper.ToString(pair.Value.Item2) + " " + string.Join(" ", pair.Value.Item1.Select(s => s.ToString())));
            }
        }

        public void Load(string file)
        {
            if (!File.Exists(file))
                return;

            foreach (string line in File.ReadLines(file))
            {
                string[] splits = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (splits.Length < 3)
                    continue;

                string type = splits[0];
                FQDN key = new FQDN(splits[1]);
                DateTime date = DateHelper.FromString(splits[2]);

                if (type == "NS")
                    _nameservers.TryAdd(key, new Tuple<FQDN[], DateTime>(splits.Skip(3).Select(s => new FQDN(s)).ToArray(), date));
                if (type == "IP")
                    _nameserverIps.TryAdd(key, new Tuple<IPAddress[], DateTime>(splits.Skip(3).Select(IPAddress.Parse).ToArray(), date));
            }
        }
    }
}