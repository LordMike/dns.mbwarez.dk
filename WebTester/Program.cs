using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using DnsLib2;
using DnsLib2.Enums;
using DnsLib2.Records;
using RestSharp;
using Shared;
using WebShared.Db;
using WebShared.Utilities;
using IpInfo = WebShared.Db.IpInfo;
using TldServer = WebShared.Db.TldServer;

namespace WebTester
{
    class Program
    {
        private const int MaxToTestPrRound = 1000;
        private const int MaxToCheckPrRound = 10000;
        private const int MaxSavePerRound = 1000;
        private static string _logFileName;
        private static StreamWriter _logWriter;

        private static bool _filterIpv6;
        private static CancellationTokenSource _cancellationToken = new CancellationTokenSource();

        private static ConsoleLogger _ravenClient;

        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: <log.txt> <ipv6 true|false>");
                return;
            }

            _ravenClient = ConsoleLogger.ApplySentry();

            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                _cancellationToken.Cancel();
                eventArgs.Cancel = true;

                Console.WriteLine("Cancellation requested");
            };

            _ravenClient?.CaptureMessage("Began run");

            // Config
            _logFileName = args[0];
            _filterIpv6 = args[1] != "true";

            _logWriter = new StreamWriter(_logFileName);

            Stopwatch sw = new Stopwatch();

            try
            {
                DateTime boundsTest = DateTime.UtcNow.AddDays(-14);
                DateTime boundsIpData = DateTime.UtcNow.AddDays(-31);

                Log("Main", string.Format("Beginning Refresh ({0:yyyy-MM-dd HH:mm:ss})", DateTime.Now));

                sw.Restart();
                HandleRefresh();
                sw.Stop();

                Log("Main", string.Format("Finished Refresh {0:N0} ms ({1:yyyy-MM-dd HH:mm:ss})", sw.ElapsedMilliseconds, DateTime.Now));

                Log("Main", string.Format("Beginning Test ({0:yyyy-MM-dd HH:mm:ss})", DateTime.Now));

                sw.Restart();
                HandleTest(boundsTest);
                sw.Stop();

                Log("Main", string.Format("Finished Test {0:N0} ms ({1:yyyy-MM-dd HH:mm:ss})", sw.ElapsedMilliseconds, DateTime.Now));

                Log("Main", string.Format("Beginning IP data update ({0:yyyy-MM-dd HH:mm:ss})", DateTime.Now));
                sw.Restart();
                HandleIpInfo(boundsIpData);
                sw.Stop();

                Log("Main", string.Format("Finished IP data update {0:N0} ms ({1:yyyy-MM-dd HH:mm:ss})", sw.ElapsedMilliseconds, DateTime.Now));

                Log("Main", "Finished");
            }
            catch (Exception ex)
            {
                _ravenClient?.CaptureException(ex);

                Log("Main", "Got exception: " + ex.Message);
                Log("Main", ex.StackTrace);

                while ((ex = ex.InnerException) != null)
                {
                    Log("Main", "== Inner ==");
                    Log("Main", ex.Message);
                    Log("Main", ex.StackTrace);
                }
            }
            finally
            {
                _logWriter.Flush();
                _logWriter.Close();
            }

            _ravenClient?.CaptureMessage("Finished run");
        }

        private static void HandleIpInfo(DateTime bounds)
        {
            // Fetch todolists
            List<IpInfo> todoItems;
            List<IpInfo> toAdd;
            HashSet<string> modified = new HashSet<string>();

            using (DnsDb db = new DnsDb())
            {
                Expression<Func<TldServer, bool>> ipFilter = s => true;
                if (_filterIpv6)
                    ipFilter = s => s.ServerType == ServerType.IPv4;

                List<string> ips = db.Servers.Where(s => s.DomainItem.IsActive).Where(ipFilter).Select(s => s.ServerIp).Distinct().ToList();

                // Filter out those that do not need updates
                List<string> filterIps = db.IpInfos.Where(s => ips.Contains(s.Ip) && s.LastUpdateUtc >= bounds).Select(s => s.Ip).ToList();

                List<string> todoIps = ips.Except(filterIps).ToList();
                todoItems = db.IpInfos.Where(s => todoIps.Contains(s.Ip)).OrderBy(s => s.LastUpdateUtc).ToList();

                toAdd = todoIps.Except(todoItems.Select(x => x.Ip)).AsEnumerable().Select(s => new IpInfo { Ip = s }).ToList();
            }

            Log("Main-ip", "IP Infoes to fetch: " + (todoItems.Count + toAdd.Count));

            RestClient rest = new RestClient(new Uri("http://ipinfo.io/"));

            foreach (IpInfo info in toAdd.Concat(todoItems))
            {
                if (_cancellationToken.IsCancellationRequested)
                    break;

                IRestResponse<IpInfo> result;
                try
                {
                    result = rest.Get<IpInfo>(new RestRequest(info.Ip));
                }
                catch (Exception ex)
                {
                    Log("Main-ip", "Got exception: " + ex.Message);
                    continue;
                }

                if (result.StatusCode != HttpStatusCode.OK)
                {
                    Log("Main-ip", "Hit a limit (code was: " + result.StatusCode + " " + result.StatusDescription + ")");
                    break;
                }

                if (result.Data == null)
                {
                    Log("Main-ip", "Got bad data (was null)");
                    continue;
                }

                info.Hostname = result.Data.Hostname;
                info.Loc = result.Data.Loc;
                info.Org = result.Data.Org;
                info.City = result.Data.City;
                info.Region = result.Data.Region;
                info.Country = result.Data.Country;
                info.Phone = result.Data.Phone;
                info.Postal = result.Data.Postal;

                info.LastUpdateUtc = DateTime.UtcNow;

                Log("Main-ip", "Updated " + info.Ip);
                modified.Add(info.Ip);
            }

            Log("Main-ip", "Done");

            Stopwatch swDb = Stopwatch.StartNew();
            using (DnsDb db = new DnsDb())
            {
                toAdd.Where(s => modified.Contains(s.Ip)).ForEach(s => db.IpInfos.Add(s));
                todoItems.Where(s => modified.Contains(s.Ip)).ForEach(s =>
                {
                    db.IpInfos.Attach(s);
                    db.Entry(s).State = EntityState.Modified;
                });
                Log("Main-ip", string.Format("Prepped DB in {0:N0} ms", swDb.ElapsedMilliseconds));

                swDb.Restart();
                db.SaveChanges();
                swDb.Stop();
                Log("Main-ip", string.Format("Saved to DB in {0:N0} ms", swDb.ElapsedMilliseconds));
            }
        }

        private static void HandleTest(DateTime bounds)
        {
            // Fetch todolists
            List<TldServer> todo;

            using (DnsDb db = new DnsDb())
            {
                IQueryable<TldServer> tmpQuery = db.Servers.Where(s => s.Test.LastCheckUtc < bounds);
                if (_filterIpv6)
                    tmpQuery = tmpQuery.Where(s => s.ServerType == ServerType.IPv4);

                todo = tmpQuery.OrderBy(s => s.Test.LastCheckUtc).Take(MaxToTestPrRound).ToList();
            }

            Log("Main-test", "Servers to (re-)test: " + todo.Count);

            DateTime boundsUpdate = DateTime.UtcNow;

            using (UdpDnsClient clientV4 = new UdpDnsClient(AddressFamily.InterNetwork))
            using (UdpDnsClient clientV6 = new UdpDnsClient(AddressFamily.InterNetworkV6))
            {
                Parallel.ForEach(todo, new ParallelOptions { MaxDegreeOfParallelism = 4 }, server =>
                {
                    if (_cancellationToken.IsCancellationRequested)
                        return;

                    Stopwatch sw = Stopwatch.StartNew();

                    IPAddress serverIp = IPAddress.Parse(server.ServerIp);
                    IPEndPoint serverEndpointDns = new IPEndPoint(serverIp, 53);
                    IPEndPoint serverEndpointFtp = new IPEndPoint(serverIp, 21);
                    IPEndPoint serverEndpointRsync = new IPEndPoint(serverIp, 873);

                    UdpDnsClient client = serverEndpointDns.AddressFamily == AddressFamily.InterNetwork ? clientV4 : clientV6;

                    Task<bool> allowsTcp = Task.Run(() => DnsUtilities.TestTcp(serverEndpointDns, server.Domain));
                    Task<bool> allowsAxfr = Task.Run(() => DnsUtilities.TestAxfr(serverEndpointDns, server.Domain));

                    Task<bool> allowsUdp = Task.Run(() => DnsUtilities.TestUdp(client, serverEndpointDns, server.Domain));
                    Task<bool> allowsNsec = Task.Run(() => DnsUtilities.TestNsec(client, QType.NSEC, serverEndpointDns, server.Domain));
                    Task<bool> allowsNsec3 = Task.Run(() => DnsUtilities.TestNsec(client, QType.NSEC3, serverEndpointDns, server.Domain));

                    Task<bool> allowsFtp = Task.Run(() => DnsUtilities.TestTcpPort(serverEndpointFtp));
                    Task<bool> allowsRsync = Task.Run(() => DnsUtilities.TestTcpPort(serverEndpointRsync));

                    server.Test.FeatureSet.SupportsTcp = allowsTcp.Result;
                    server.Test.FeatureSet.SupportsAxfr = allowsAxfr.Result;

                    server.Test.FeatureSet.SupportsUdp = allowsUdp.Result;
                    server.Test.FeatureSet.SupportsNsec = allowsNsec.Result;
                    server.Test.FeatureSet.SupportsNsec3 = allowsNsec3.Result;

                    server.Test.FeatureSet.SupportsTcpFtp = allowsFtp.Result;
                    server.Test.FeatureSet.SupportsTcpRsync = allowsRsync.Result;

                    server.Test.LastCheckUtc = DateTime.UtcNow;

                    sw.Stop();

                    Log("Main-test", "Tested: " + server.Domain + " (" + server.ServerName + " - " + server.ServerIp + ") " + sw.ElapsedMilliseconds + " ms");
                });
            }

            List<TldServer> toSave = todo.Where(s => s.Test.LastCheckUtc >= boundsUpdate).ToList();
            Log("Main-test", "Saving: " + toSave.Count + " servers to DB");

            Stopwatch swDb = new Stopwatch();
            using (DnsDb db = new DnsDb())
            {
                for (int i = 0; ; i += MaxSavePerRound)
                {
                    List<TldServer> thisRound = toSave.Skip(i).Take(MaxSavePerRound).ToList();
                    if (!thisRound.Any())
                        break;

                    swDb.Restart();
                    thisRound.ForEach(server =>
                    {
                        db.Servers.Attach(server);
                        db.Entry(server).ComplexProperty(s => s.Test).IsModified = true;
                    });

                    swDb.Stop();
                    Log("Main-test", string.Format("Prepped {0:N0} items for DB in {1:N0} ms", thisRound.Count, swDb.ElapsedMilliseconds));

                    swDb.Restart();
                    db.SaveChanges();
                    swDb.Stop();
                    Log("Main-test", string.Format("Saved {0:N0} items to DB in {1:N0} ms", thisRound.Count, swDb.ElapsedMilliseconds));
                }
            }
        }

        private static void HandleRefresh()
        {
            // Fetch todolists
            List<TldServer> todo;

            using (DnsDb db = new DnsDb())
            {
                const int minTime = 3600;
                const int maxTime = 86400;

                IQueryable<TldServer> tmpQuery = db.Domains.Where(s => s.IsActive && s.Servers.Any(x => DbFunctions.AddSeconds(x.Refresh.LastCheckUtc, x.Refresh.RefreshTime < minTime ? minTime : x.Refresh.RefreshTime > maxTime ? maxTime : x.Refresh.RefreshTime) < DateTime.UtcNow)).SelectMany(s => s.Servers);
                if (_filterIpv6)
                    tmpQuery = tmpQuery.Where(s => s.ServerType == ServerType.IPv4);

                todo = tmpQuery.OrderBy(s => s.Refresh.LastCheckUtc).Take(MaxToCheckPrRound).ToList();
            }

            Log("Main-refresh", "Servers to refresh  : " + todo.Count);

            DateTime boundsUpdate = DateTime.UtcNow;

            using (UdpDnsClient clientV4 = new UdpDnsClient(AddressFamily.InterNetwork))
            using (UdpDnsClient clientV6 = new UdpDnsClient(AddressFamily.InterNetworkV6))
            {
                Parallel.ForEach(todo, new ParallelOptions { MaxDegreeOfParallelism = 4 }, server =>
                {
                    if (_cancellationToken.IsCancellationRequested)
                        return;

                    Stopwatch sw = Stopwatch.StartNew();

                    IPAddress serverIp = IPAddress.Parse(server.ServerIp);
                    UdpDnsClient client = serverIp.AddressFamily == AddressFamily.InterNetwork ? clientV4 : clientV6;

                    RecordViewSOA soa = DnsUtilities.FetchSoaFromAuthorative(client, new IPEndPoint(serverIp, 53), server.Domain);

                    server.Refresh.LastCheckUtc = DateTime.UtcNow;
                    server.Refresh.LastCheckSuccess = soa != null;

                    if (soa != null)
                    {
                        server.Refresh.Serial = soa.Serial;
                        server.Refresh.MasterResponsibleName = soa.ResponsibleName.Name;
                        server.Refresh.MasterServerDnsName = soa.MasterName.Name;
                        server.Refresh.RefreshTime = (int)soa.Refresh;
                    }

                    sw.Stop();

                    Log("Main-refresh", "Refreshed: " + server.Domain + " (" + server.ServerName + " - " + server.ServerIp + ") " + sw.ElapsedMilliseconds + " ms");
                });
            }

            List<TldServer> toSave = todo.Where(s => s.Refresh.LastCheckUtc >= boundsUpdate).ToList();
            Log("Main-refresh", "Saving: " + toSave.Count + " servers to DB");

            Stopwatch swDb = new Stopwatch();
            using (DnsDb db = new DnsDb())
            {
                for (int i = 0; ; i += MaxSavePerRound)
                {
                    Dictionary<string, Tuple<string, string, int>> domainUpdates = new Dictionary<string, Tuple<string, string, int>>();

                    if (!toSave.Skip(i).Any())
                        break;

                    swDb.Restart();
                    toSave.Skip(i).Take(MaxSavePerRound).ForEach(server =>
                    {
                        db.Servers.Attach(server);
                        db.Entry(server).ComplexProperty(s => s.Refresh).IsModified = true;

                        if (server.Refresh.LastCheckSuccess)
                            domainUpdates[server.Domain] = new Tuple<string, string, int>(server.Refresh.MasterResponsibleName, server.Refresh.MasterServerDnsName, server.Refresh.RefreshTime);
                    });

                    List<string> domainNames = domainUpdates.Keys.ToList();
                    List<TldDomain> domainItems = db.Domains.Where(s => domainNames.Contains(s.Domain)).ToList();

                    foreach (TldDomain domain in domainItems)
                    {
                        Tuple<string, string, int> updateItem;
                        if (!domainUpdates.TryGetValue(domain.Domain, out updateItem))
                            continue;

                        domain.MasterResponsibleName = updateItem.Item1;
                        domain.MasterServerDnsName = updateItem.Item2;
                        domain.SoaRefreshTime = updateItem.Item3;
                    }
                    swDb.Stop();
                    Log("Main-refresh", string.Format("Prepped {0:N0} (at {1:N0} of {2:N0}) items for DB in {3:N0} ms", domainItems.Count, i + domainItems.Count, toSave.Count, swDb.ElapsedMilliseconds));

                    swDb.Restart();
                    db.SaveChanges();
                    swDb.Stop();
                    Log("Main-refresh", string.Format("Saved {0:N0} (at {1:N0} of {2:N0}) items to DB in {3:N0} ms", domainItems.Count, i + domainItems.Count, toSave.Count, swDb.ElapsedMilliseconds));
                }
            }
        }

        private static void Log(string category, string line)
        {
            string logLine = "[" + category + "] " + line;

            Console.WriteLine(logLine);
            _logWriter.WriteLine(logLine);
            _logWriter.Flush();
        }
    }
}
