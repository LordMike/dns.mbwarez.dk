using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using DnsLib2;
using DnsLib2.Common;
using DnsLib2.Enums;
using DnsLib2.Records;
using Shared;
using WebShared.Db;
using WebShared.Utilities;

namespace TldScraper
{
    class Program
    {
        private static CancellationTokenSource _cancellationToken = new CancellationTokenSource();

        private static ConsoleLogger _ravenClient;

        static void Main(string[] args)
        {
            IPEndPoint recursiveServer = new IPEndPoint(IPAddress.Parse("192.168.1.21"), 53);

            _ravenClient = ConsoleLogger.ApplySentry();

            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                _cancellationToken.Cancel();
                eventArgs.Cancel = true;

                Console.WriteLine("Cancellation requested");
            };
            
            _ravenClient?.CaptureMessage("Began run");

            // Find eligible domains
            DateTime boundsDate = DateTime.UtcNow.AddDays(-14);

            List<TldDomain> todo;
            using (DnsDb db = new DnsDb())
            {
                db.Database.CommandTimeout = 1200;

                // Filter to AXFR / NSEC
                var eligible = db.Domains.Where(s => s.Servers.Any(x => x.Test.FeatureSet.SupportsAxfr || x.Test.FeatureSet.SupportsNsec));
                eligible = eligible.Where(s => !s.ScrapeHistory.Any() || s.ScrapeHistory.Max(x => x.ScanTimeUtc) < boundsDate);

                todo = eligible.Include(s => s.Servers).ToList();
            }

            Console.WriteLine("Got " + todo.Count + " eligible domains");

            foreach (TldDomain domain in todo)
            {
                if (_cancellationToken.IsCancellationRequested)
                    break;

                if (domain.Servers.Any(x => x.Test.FeatureSet.SupportsAxfr))
                {
                    HandleAxfr(domain);

                    continue;
                }

                if (domain.Servers.Any(x => x.Test.FeatureSet.SupportsNsec))
                {
                    HandleNsec(domain);

                    continue;
                }
            }

            _ravenClient?.CaptureMessage("Finished run");
        }

        private static void HandleAxfr(TldDomain domain)
        {
            ConcurrentDictionary<FQDN, HashSet<FQDN>> nameservers = new ConcurrentDictionary<FQDN, HashSet<FQDN>>();

            Console.WriteLine("AXFR beginning " + domain.Domain + ", " + domain.Servers.Count(s => s.Test.FeatureSet.SupportsAxfr) + " servers");

            try
            {
                bool success = false;

                foreach (TldServer server in domain.Servers.Where(s => s.Test.FeatureSet.SupportsAxfr))
                {
                    IPEndPoint point = new IPEndPoint(IPAddress.Parse(server.ServerIp), 53);

                    try
                    {
                        List<DnsRecordView> axfr = DnsUtilities.DoAxfr(point, domain.Domain);

                        foreach (RecordViewNS recordView in axfr.OfType<RecordViewNS>())
                        {
                            HashSet<FQDN> list = nameservers.GetOrAdd(recordView.QName, fqdn => new HashSet<FQDN>());
                            list.Add(recordView.Reference);
                        }

                        // Check no more
                        success = true;
                        break;
                    }
                    catch (Exception ex)
                    {
                        ex.Data.Add("Domain", domain.Domain);
                        ex.Data.Add("DomainServer", point.ToString());

                        _ravenClient?.CaptureException(ex, level: ConsoleLoggerErrorLevel.Warning);

                        Console.WriteLine("AXFR Failed " + point + ", " + domain.Domain + ": " + ex.Message);
                    }
                }

                if (!success)
                    return;
            }
            catch (Exception ex)
            {
                ex.Data.Add("Domain", domain.Domain);
                _ravenClient?.CaptureException(ex, level: ConsoleLoggerErrorLevel.Warning);

                Console.WriteLine("AXFR Failed " + domain.Domain + ": " + ex.Message);
                return;
            }

            Console.WriteLine("AXFR got " + nameservers.Count + " domains for " + domain.Domain);

            int updated = SaveData(domain, new HashSet<FQDN>(nameservers.Keys), nameservers);

            Console.WriteLine("AXFR saved " + updated + " domains for " + domain.Domain);

            SaveCompletedRun(domain, "AXFR");
        }

        private static void HandleNsec(TldDomain domain)
        {
            const int threadsCount = 16;
            const int maxLookups = 20;

            Console.WriteLine("NSEC beginning " + domain.Domain + ", " + domain.Servers.Count(s => s.Test.FeatureSet.SupportsNsec) + " servers");

            IEnumerable<IPEndPoint> tldServers = domain.Servers.Select(s => new IPEndPoint(IPAddress.Parse(s.ServerIp), 53));
            ValueCycler<IPEndPoint> servers = new ValueCycler<IPEndPoint>(tldServers.Where(s => s.AddressFamily == AddressFamily.InterNetwork));

            FQDN start = new FQDN(domain.Domain);

            // Load
            HashSet<FQDN> checkedInThisRun = new HashSet<FQDN>();
            HashSet<FQDN> allSeen = new HashSet<FQDN>();
            HashSet<FQDN> searchNsec = new HashSet<FQDN>();
            HashSet<FQDN> toSave = new HashSet<FQDN>();
            ConcurrentDictionary<FQDN, HashSet<FQDN>> nameservers = new ConcurrentDictionary<FQDN, HashSet<FQDN>>();
            ConcurrentQueue<FQDN> todoZones = new ConcurrentQueue<FQDN>();
            ConcurrentDictionary<FQDN, int> lookups = new ConcurrentDictionary<FQDN, int>();

            int requests = 0;

            using (DnsDb db = new DnsDb())
            {
                db.Database.CommandTimeout = 1200;

                foreach (string fqdn in db.ScrapedDomains.Where(s => s.ParentFqdn == domain.Domain).Select(s => s.Fqdn))
                {
                    FQDN item = new FQDN(fqdn);
                    todoZones.Enqueue(item);
                    allSeen.Add(item);
                }
            }

            if (todoZones.Count < threadsCount)
            {
                todoZones.Enqueue(start);

                // Add a series of starting-points
                for (char a = '0'; a <= '9'; a++)
                    todoZones.Enqueue(new FQDN(a + "." + domain.Domain));
                for (char a = 'a'; a <= 'z'; a++)
                    todoZones.Enqueue(new FQDN(a + "." + domain.Domain));
            }

            SemaphoreSlim running = new SemaphoreSlim(threadsCount);

            Thread[] threads = new Thread[threadsCount];
            for (int i = 0; i < threads.Length; i++)
            {
                threads[i] = new Thread(() =>
                {
                    running.Wait();

                    try
                    {
                        BuildOptRecord ednsRecord = new BuildOptRecord(4096, 0x00008000);
                        DnsMessageBuilder builder = new DnsMessageBuilder()
                            .SetFlag(DnsFlags.AuthenticData)
                            .SetOpCode(DnsOpcode.Query)
                            .AddAdditional(ednsRecord);

                        IPEndPoint server = servers.GetNext();

                        using (UdpDnsClient client = new UdpDnsClient(AddressFamily.InterNetwork))
                        {
                            while (true)
                            {
                                if (_cancellationToken.IsCancellationRequested)
                                    return;

                                FQDN todo;
                                if (!todoZones.TryDequeue(out todo))
                                    break;

                                int currentLookups = lookups.AddOrUpdate(todo, 1, (fqdn, i1) => i1 + 1);
                                if (currentLookups > maxLookups)
                                {
                                    Console.WriteLine("Reached max lookups in NSEC for " + todo);
                                    break;
                                }

                                // Query zone
                                Task<DnsMessageView> respTask;
                                if (searchNsec.Remove(todo))
                                    // Previous run wants this to be an NSEC query only
                                    respTask = client.Query(server, builder.SetQuestion(todo, QType.NSEC));
                                else
                                    respTask = client.Query(server, builder.SetQuestion(todo, QType.ANY));

                                Interlocked.Increment(ref requests);

                                DnsMessageView resp;
                                try
                                {
                                    resp = respTask.Result;
                                }
                                catch (AggregateException ex)
                                {
                                    Exception inner = ex.InnerException;

                                    inner.Data.Add("DomainServer", server.ToString());
                                    inner.Data.Add("Query", todo.ToString());

                                    _ravenClient?.CaptureException(inner, level: ConsoleLoggerErrorLevel.Warning);

                                    if (inner is TaskCanceledException)
                                    {
                                        // Timeout - requeue 
                                        Console.WriteLine("Timed out: " + todo);
                                        todoZones.Enqueue(todo);
                                        server = servers.GetNext();
                                        continue;
                                    }

                                    throw;
                                }

                                if (resp.Header.ResponseCode == ResponseCode.NoError)
                                {
                                    foreach (RecordViewNS recordView in resp.AllRecords().OfType<RecordViewNS>().Where(s => s.QName.Equals(todo)))
                                    {
                                        HashSet<FQDN> list = nameservers.GetOrAdd(recordView.QName, fqdn => new HashSet<FQDN>());

                                        lock (list)
                                            list.Add(recordView.Reference);

                                        lock (toSave)
                                            toSave.Add(todo);
                                    }
                                }

                                // Process
                                bool hadAnyNsec = false;
                                foreach (RecordViewNSEC nsec in resp.AllRecords().OfType<RecordViewNSEC>())
                                {
                                    hadAnyNsec = true;

                                    lock (checkedInThisRun)
                                        if (checkedInThisRun.Add(nsec.NextName) && nsec.NextName.IsSubdomainOf(start))
                                            todoZones.Enqueue(nsec.NextName);

                                    lock (allSeen)
                                        if (allSeen.Add(nsec.NextName))
                                            Console.WriteLine("New zone: " + nsec.QName + " => " + nsec.NextName + ", reqs so far: " + requests);

                                    if (!nsec.NextName.IsValidFqdn)
                                        Console.WriteLine("!! Got invalid FQDN: " + nsec.NextName + ", reqs so far: " + requests);
                                }

                                if (!hadAnyNsec)
                                {
                                    // Requeue
                                    Console.WriteLine("Requeueing blank zone: " + todo + " was against " + server + ", reqs so far: " + requests);
                                    todoZones.Enqueue(todo);
                                    searchNsec.Add(todo);
                                    server = servers.GetNext();
                                }
                            }
                        }
                    }
                    finally
                    {
                        running.Release();
                    }
                });
            }

            for (int i = 0; i < threads.Length; i++)
                threads[i].Start();

            int updated = 0;
            {
                HashSet<FQDN> tmp = new HashSet<FQDN>();

                do
                {
                    Thread.Sleep(2000);

                    lock (toSave)
                    {
                        if (toSave.Count < 2000)
                            continue;

                        foreach (FQDN fqdn in toSave.Take(2000))
                            tmp.Add(fqdn);

                        foreach (FQDN fqdn in tmp)
                            toSave.Remove(fqdn);
                    }

                    int saved = SaveData(domain, tmp, nameservers);
                    updated += saved;

                    tmp.Clear();

                    Console.WriteLine("NSEC Saved " + saved + " to DB");
                } while (running.CurrentCount != threadsCount);
            }

            for (int i = 0; i < threads.Length; i++)
                threads[i].Join();

            // Save
            Console.WriteLine("NSEC got " + nameservers.Count + " domains for " + domain.Domain + " in " + requests + " requests");

            updated += SaveData(domain, toSave, nameservers);

            Console.WriteLine("NSEC saved " + updated + " domains for " + domain.Domain);

            SaveCompletedRun(domain, "NSEC");
        }

        private static void HandleNsec3(TldDomain domain)
        {

        }

        private static int SaveData(TldDomain domain, HashSet<FQDN> keys, ConcurrentDictionary<FQDN, HashSet<FQDN>> records)
        {
            int updated = 0;
            using (DnsDb db = new DnsDb())
            {
                db.Database.CommandTimeout = 60000;

                HashSet<FQDN> toUpdate = new HashSet<FQDN>(keys);

                List<string> doms = new List<string>(keys.Select(s => s.ToString()));
                List<Domain> items = db.ScrapedDomains.Where(s => doms.Contains(s.Fqdn)).ToList();

                // Update already added
                foreach (Domain item in items)
                {
                    FQDN key = new FQDN(item.Fqdn);

                    HashSet<FQDN> list = records[key];
                    toUpdate.Remove(key);

                    item.LastNameservers = string.Join(";", list.Select(s => s.ToString()).OrderBy(s => s));
                    item.LastSeenUtc = DateTime.UtcNow;

                    updated++;
                }

                // Add the rest
                foreach (FQDN fqdn in toUpdate)
                {
                    if (!fqdn.IsValidFqdn)
                        continue;

                    HashSet<FQDN> values = records[fqdn];

                    Domain item = db.ScrapedDomains.Add(new Domain());

                    item.Fqdn = fqdn.ToString();
                    item.ParentFqdn = domain.Domain;
                    lock (values)
                        item.LastNameservers = string.Join(";", values.Select(s => s.ToString()).OrderBy(s => s));
                    item.LastSeenUtc = DateTime.UtcNow;

                    updated++;
                }

                try
                {
                    db.SaveChanges();
                }
                catch (DbEntityValidationException ex)
                {
                    // Dummy catch to allow debugging ... -.-'
                    throw;
                }
            }

            return updated;
        }

        private static void SaveCompletedRun(TldDomain domain, string type)
        {
            using (DnsDb db = new DnsDb())
            {
                db.Database.CommandTimeout = 1200;

                db.ScrapedDomainsHistory.Add(new DomainScrapeHistory
                {
                    Fqdn = domain.Domain,
                    ScanTimeUtc = DateTime.UtcNow,
                    ScanType = type
                });

                try
                {
                    db.SaveChanges();
                }
                catch (DbEntityValidationException ex)
                {
                    // Dummy catch to allow debugging ... -.-'
                    throw;
                }
            }
        }
    }
}
