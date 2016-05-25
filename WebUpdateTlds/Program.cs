using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Data.Entity;
using System.Reflection;
using DnsLib2.Common;
using DnsLib2.Records;
using SharpRaven;
using WebShared.Db;
using WebShared.Utilities;
using TldInfo = WebShared.Db.TldInfo;
using TldServer = WebShared.Db.TldServer;

namespace WebUpdateTlds
{
    internal class Program
    {
        private static string _nameserverCacheFile;
        private static string _tldConfigFile;
        private static string _logFileName;

        private static CancellationTokenSource _cancellationToken = new CancellationTokenSource();

        private static StreamWriter _logWriter;

        private static RavenClient _ravenClient;

        private static void Main(string[] args)
        {
            if (args.Length != 4)
            {
                Console.WriteLine("Usage: <recursive-dnsserver> <nameservercache.txt> <config_tld.txt> <log.txt>");
                return;
            }

            string ravenDsn = ConfigurationManager.AppSettings["sentry_dsn"];
            if (ravenDsn != null)
            {
                _ravenClient = new RavenClient(ravenDsn);
                _ravenClient.Logger = Assembly.GetExecutingAssembly().GetName().Name;
            }

            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                _cancellationToken.Cancel();
                eventArgs.Cancel = true;

                Console.WriteLine("Cancellation requested");
            };

            AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
            {
                Exception exception = eventArgs.ExceptionObject as Exception;
                if (exception == null)
                    return;

                _ravenClient?.CaptureException(exception);
            };

            _ravenClient?.CaptureMessage("Began run");

            // Config
            IPEndPoint recursiveServer = new IPEndPoint(IPAddress.Parse(args[0]), 53);
            _nameserverCacheFile = args[1];
            _tldConfigFile = args[2];
            _logFileName = args[3];

            _logWriter = new StreamWriter(_logFileName);

            try
            {
                Log("Main", "Beginning");

                // Load nameserver cache
                NameserverCache nameserverCache = new NameserverCache(recursiveServer);
                if (File.Exists(_nameserverCacheFile))
                    nameserverCache.Load(_nameserverCacheFile);

                // Load current config
                TldConfigCollection configs = TldConfigCollection.FromFile(_tldConfigFile);

                // Load current rootzone
                List<FQDN> roots = FetchRootZone(nameserverCache);
                List<FQDN> secondLevels = roots.Select(configs.Get)
                        .NotNull()
                        .SelectMany(s => s.SecondLevels.Select(x => new FQDN(x)))
                        .ToList();

                Log("Main", "Loaded: " + roots.Count + " roots, " + secondLevels.Count + " second-levels");
                Log("Main", "Loaded: " + configs.Count + " TLD configs");

                using (DnsDb db = new DnsDb())
                {
                    db.Database.ExecuteSqlCommand("UPDATE TldDomains SET IsActive = 0");

                    foreach (FQDN fqdn in roots.Concat(secondLevels))
                    {
                        if (_cancellationToken.IsCancellationRequested)
                            break;

                        string domain = fqdn.Name;
                        TldDomain domainItem = db.Domains.Include(s => s.Servers).SingleOrDefault(s => s.Domain == domain);
                        if (domainItem == null)
                        {
                            domainItem = new TldDomain();
                            domainItem.Domain = domain;
                            domainItem.ParentTld = new FQDN(string.Join(".", fqdn.GetLabels().Skip(1))).Name;
                            domainItem.DomainLevel = fqdn.GetLabels().Count();
                            domainItem.CreatedTime = DateTime.UtcNow;
                            domainItem.Info = new TldInfo();

                            db.Domains.Add(domainItem);

                            Log("Main", "Adding " + fqdn.Name);
                        }

                        // Update domain
                        domainItem.IsActive = true;

                        TldConfig config = configs.Get(fqdn);
                        if (config != null)
                        {
                            bool updated = false;

                            // Only set if necessary
                            if (domainItem.Info.Type != config.Type)
                            {
                                domainItem.Info.Type = config.Type;
                                updated = true;
                            }

                            if (domainItem.Info.Wikipage != config.Wikipage)
                            {
                                domainItem.Info.Wikipage = config.Wikipage;
                                updated = true;
                            }

                            if (updated)
                            {
                                domainItem.Info.LastUpdateUtc = DateTime.UtcNow;

                                Log("Main", "Updated info for " + fqdn.Name);
                            }
                        }

                        // Update servers
                        List<IPAddress> expectedServers = nameserverCache.GetDomainNameserverIps(fqdn).ToList();
                        ICollection<TldServer> servers = domainItem.Servers;

                        IEnumerable<TldServer> toRemove = servers.Where(s => !expectedServers.Select(x => x.ToString()).Contains(s.ServerIp));
                        foreach (TldServer server in toRemove)
                        {
                            domainItem.Servers.Remove(server);

                            Log("Main", "Removing server from " + fqdn.Name);
                        }

                        foreach (FQDN nameserver in nameserverCache.GetDomainNameservers(fqdn))
                            foreach (IPAddress address in nameserverCache.GetNameserverIps(nameserver))
                            {
                                TldServer serverItem = domainItem.Servers.SingleOrDefault(s => s.ServerIp == address.ToString());
                                if (serverItem == null)
                                {
                                    serverItem = new TldServer();
                                    serverItem.Domain = fqdn.Name;
                                    serverItem.ServerIp = address.ToString();
                                    serverItem.CreatedTime = DateTime.UtcNow;

                                    switch (address.AddressFamily)
                                    {
                                        case AddressFamily.InterNetwork:
                                            serverItem.ServerType = ServerType.IPv4;
                                            break;
                                        case AddressFamily.InterNetworkV6:
                                            serverItem.ServerType = ServerType.IPv6;
                                            break;
                                    }

                                    serverItem.Refresh = new ServerSoaRefresh();
                                    serverItem.Test = new ServerTest();
                                    serverItem.Test.FeatureSet = new ServerFeatureSet();

                                    domainItem.Servers.Add(serverItem);

                                    Log("Main", "Adding server to " + fqdn.Name);
                                }

                                // Update server
                                serverItem.ServerName = nameserver.Name;
                            }
                    }

                    Log("Main", "Saving to DB");
                    db.SaveChanges();
                }

                // Save
                nameserverCache.Save(_nameserverCacheFile);

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

        private static List<FQDN> FetchRootZone(NameserverCache cache)
        {
            string rootFile = "cache_rootzone.bin";
            string rootServer = "xfr.lax.dns.icann.org";

            if (!File.Exists(rootFile) || new FileInfo(rootFile).LastWriteTimeUtc < DateTime.UtcNow.AddDays(-3))
            {
                // Refresh
                IPAddress rootIp = Dns.GetHostAddresses(rootServer).First(s => s.AddressFamily == AddressFamily.InterNetwork);
                IPEndPoint rootEndpoint = new IPEndPoint(rootIp, 53);
                List<DnsRecordView> rootZoneRecords = DnsUtilities.DoAxfr(rootEndpoint, ".");

                foreach (DnsRecordView record in rootZoneRecords)
                    cache.RecordDnsAnswer(record);

                List<FQDN> rootTlds = rootZoneRecords.OfType<RecordViewNS>().Select(s => s.QName).Distinct().ToList();

                File.WriteAllLines(rootFile, rootTlds.Select(s => s.ToString()));
                return rootTlds;
            }

            return File.ReadLines(rootFile).Select(s => new FQDN(s)).ToList();
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
