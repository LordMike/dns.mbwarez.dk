using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Data.Entity;
using System.Dynamic;
using System.Runtime.Serialization.Formatters.Binary;
using DnsLib2.Common;
using DnsLib2.Enums;
using DnsLib2.Records;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shared;
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

        private static ConsoleLogger _ravenClient;

        private static void Main(string[] args)
        {
            if (args.Length != 4)
            {
                Console.WriteLine("Usage: <recursive-dnsserver> <nameservercache.txt> <config_tld.txt> <log.txt>");
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
            IPEndPoint recursiveServer = new IPEndPoint(IPAddress.Parse(args[0]), 53);
            _nameserverCacheFile = args[1];
            _tldConfigFile = args[2];
            _logFileName = args[3];

            JsonConvert.DefaultSettings = () =>
            {
                var settings = new JsonSerializerSettings();
                //settings.Converters.Add(new IPAddressConverter());
                //settings.Converters.Add(new FqdnConverter());
                //settings.Converters.Add(new BaseConverter());

                return settings;
            };

            _logWriter = new StreamWriter(_logFileName);

            try
            {
                Log("Main", "Beginning");

                // Load nameserver cache
                NameserverCache nameserverCache = new NameserverCache(recursiveServer);
                //if (File.Exists(_nameserverCacheFile))
                //    nameserverCache.Load(_nameserverCacheFile);

                // Load current config
                TldConfigCollection configs = TldConfigCollection.FromFile(_tldConfigFile);

                // Load current rootzone
                List<FQDN> roots = FetchRootZone(nameserverCache);

                List<FQDN> secondLevels = roots.Select(configs.Get)
                        .NotNull()
                        .SelectMany(s => s.SecondLevels.Select(x => new FQDN(x)))
                        .ToList();

                Log("Main", $"Loaded: {roots.Count:N0} roots, {secondLevels.Count:N0} second-levels");
                Log("Main", $"Loaded: {configs.Count:N0} TLD configs");

                using (DnsDb db = new DnsDb())
                {
                    HashSet<string> allCurrentDomains = new HashSet<string>(db.Domains.Select(s => s.Domain));

                    foreach (FQDN fqdn in roots.Concat(secondLevels))
                    {
                        if (_cancellationToken.IsCancellationRequested)
                            break;

                        allCurrentDomains.Remove(fqdn.Name);

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

                            Log("Main", $"Adding {fqdn.Name}");
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

                                Log("Main", $"Updated info for {fqdn.Name}");
                            }
                        }

                        // Update servers
                        Dictionary<string, TldServer> currentServers = domainItem.Servers.ToDictionary(s => s.ServerIp);

                        foreach (FQDN nameserver in nameserverCache.GetDomainNameservers(fqdn))
                            foreach (IPAddress address in nameserverCache.GetNameserverIps(nameserver))
                            {
                                currentServers.Remove(address.ToString());

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

                                    Log("Main", $"Adding server {fqdn.Name} / {serverItem.ServerIp}");
                                }

                                // Update server
                                serverItem.ServerName = nameserver.Name;
                            }

                        // Remove old servers
                        foreach (var server in currentServers.Values)
                        {
                            domainItem.Servers.Remove(server);

                            Log("Main", $"Removing server {fqdn.Name} / {server.ServerIp}");
                        }
                    }

                    // Deactivate old domains
                    if (!_cancellationToken.IsCancellationRequested)
                    {
                        foreach (string domain in allCurrentDomains)
                        {
                            TldDomain dom = db.Domains.Single(s => s.Domain == domain);
                            dom.IsActive = false;
                        }
                    }

                    Log("Main", "Saving to DB");
                    db.SaveChanges();
                }

                // Save
                // nameserverCache.Save(_nameserverCacheFile);

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

            List<DnsRecordView> rootZoneRecords;
            //if (!File.Exists(rootFile) || new FileInfo(rootFile).LastWriteTimeUtc < DateTime.UtcNow.AddDays(-3))
            //{
            // Refresh
            IPAddress rootIp = Dns.GetHostAddresses(rootServer).First(s => s.AddressFamily == AddressFamily.InterNetwork);
            IPEndPoint rootEndpoint = new IPEndPoint(rootIp, 53);
            rootZoneRecords = DnsUtilities.DoAxfr(rootEndpoint, ".");

            //File.WriteAllText(rootFile, JsonConvert.SerializeObject(rootZoneRecords.Select(s => s.)));
            //SerializeToFile(rootZoneRecords, rootFile);
            //}
            //else
            //{
            //    //rootZoneRecords = JsonConvert.DeserializeObject<List<DnsRecordView>>(File.ReadAllText(rootFile));
            //    rootZoneRecords = DeserializeFromFile<List<DnsRecordView>>(rootFile);
            //}

            foreach (DnsRecordView record in rootZoneRecords)
                cache.RecordDnsAnswer(record);

            List<FQDN> rootTlds = rootZoneRecords.OfType<RecordViewNS>().Select(s => s.QName).Distinct().ToList();

            return rootTlds;
        }

        private static void Log(string category, string line)
        {
            string logLine = "[" + category + "] " + line;

            Console.WriteLine(logLine);
            _logWriter.WriteLine(logLine);
            _logWriter.Flush();
        }

        private static TData DeserializeFromFile<TData>(string file)
        {
            byte[] b = File.ReadAllBytes(file);
            using (var stream = new MemoryStream(b))
            {
                var formatter = new BinaryFormatter();
                stream.Seek(0, SeekOrigin.Begin);
                return (TData)formatter.Deserialize(stream);
            }
        }

        private static void SerializeToFile<TData>(TData settings, string file)
        {
            using (var stream = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(stream, settings);
                stream.Flush();
                stream.Position = 0;

                File.WriteAllBytes(file, stream.ToArray());
            }
        }
    }

    //public class BaseConverter : JsonConverter
    //{
    //    static JsonSerializerSettings SpecifiedSubclassConversion = new JsonSerializerSettings()
    //    {
    //        //ContractResolver = new BaseSpecifiedConcreteClassConverter()
    //    };

    //    public override bool CanConvert(Type objectType)
    //    {
    //        return (objectType == typeof(DnsRecordView));
    //    }

    //    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    //    {
    //        JObject jo = JObject.Load(reader);
    //        Type type = DnsRecordView.GetRecordViewType((QType)jo["QType"].Value<int>());

    //        return JsonConvert.DeserializeObject(jo.ToString(), type);
    //    }

    //    public override bool CanWrite
    //    {
    //        get { return false; }
    //    }

    //    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    //    {
    //        throw new NotImplementedException(); // won't be called because CanWrite returns false
    //    }
    //}

    //public class FqdnConverter : JsonConverter
    //{
    //    public override bool CanConvert(Type objectType)
    //    {
    //        return (objectType == typeof(FQDN));
    //    }

    //    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    //    {
    //        FQDN val = (FQDN)value;
    //        writer.WriteValue(val.Name);
    //    }

    //    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    //    {
    //        JToken token = JToken.Load(reader);
    //        return new FQDN(token.Value<string>());
    //    }
    //}

    //public class IPAddressConverter : JsonConverter
    //{
    //    public override bool CanConvert(Type objectType)
    //    {
    //        return (objectType == typeof(IPAddress));
    //    }

    //    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    //    {
    //        IPAddress ip = (IPAddress)value;
    //        writer.WriteValue(ip.ToString());
    //    }

    //    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    //    {
    //        JToken token = JToken.Load(reader);
    //        return IPAddress.Parse(token.Value<string>());
    //    }
    //}
}
