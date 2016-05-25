using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using DnsMbwarezDk.Models.Data;
using WebShared.Db;
using IpInfo = WebShared.Db.IpInfo;
using TldServer = WebShared.Db.TldServer;

namespace DnsMbwarezDk.Code
{
    public static class DataManager
    {
        public static IpInfo GetIpInfo(string ip)
        {
            using (DnsDb db = new DnsDb())
                return db.IpInfos.SingleOrDefault(s => s.Ip == ip) ?? new IpInfo { Ip = ip };
        }

        public static List<IpInfo> GetIpInfo(List<string> ips)
        {
            using (DnsDb db = new DnsDb())
                return db.IpInfos.Where(s => ips.Contains(s.Ip)).ToList();
        }

        public static List<string> GetChildsForParent(string parent)
        {
            using (DnsDb db = new DnsDb())
                return db.Domains.Where(s => s.ParentTld == parent).Select(s => s.Domain).ToList();
        }

        public static DateTime GetLastUpdate()
        {
            using (DnsDb db = new DnsDb())
                return db.Servers.OrderByDescending(s => s.Refresh.LastCheckUtc).Select(s => s.Refresh.LastCheckUtc).FirstOrDefault();
        }

        public static List<TldDomain> SearchDomains(string filterText, bool filterIssues, bool filterLevelFirst, bool filterLevelSecond, bool filterAxfr, bool filterNsec, bool filterNsec3, bool filterFtp, bool filterRsync)
        {
            using (DnsDb db = new DnsDb())
            {
                IQueryable<TldDomain> domains = db.Domains.Include(s => s.Servers).Where(s => s.Servers.Any() && s.IsActive);

                if (!string.IsNullOrEmpty(filterText))
                {
                    string filterFromEmail = filterText.Replace("@", ".");
                    domains = domains.Where(s => s.Domain.Contains(filterText) ||
                                                s.Servers.Any(x => x.Refresh.MasterServerDnsName.Contains(filterText)) ||
                                                s.Servers.Any(x => x.Refresh.MasterResponsibleName.Contains(filterFromEmail)) ||
                                                s.Servers.Any(x => x.ServerName.Contains(filterText)) ||
                                                s.Servers.Any(x => x.ServerIp.Contains(filterText)));
                }

                if (filterIssues)
                    domains = domains.Where(s => s.Servers.Any(x => !x.Refresh.LastCheckSuccess));

                if (filterLevelFirst && filterLevelSecond)
                    domains = domains.Where(s => s.DomainLevel <= 1 || s.DomainLevel == 2);
                else if (filterLevelFirst)
                    domains = domains.Where(s => s.DomainLevel <= 1);
                else if (filterLevelSecond)
                    domains = domains.Where(s => s.DomainLevel == 2);

                if (filterAxfr)
                    domains = domains.Where(s => s.Servers.Any(x => x.Test.FeatureSet.SupportsAxfr));

                if (filterNsec && filterNsec3)
                    // Special case, make it an OR
                    domains = domains.Where(s => s.Servers.Any(x => x.Test.FeatureSet.SupportsNsec) || s.Servers.Any(x => x.Test.FeatureSet.SupportsNsec3));
                else if (filterNsec)
                    domains = domains.Where(s => s.Servers.Any(x => x.Test.FeatureSet.SupportsNsec));
                else if (filterNsec3)
                    domains = domains.Where(s => s.Servers.Any(x => x.Test.FeatureSet.SupportsNsec3));

                if (filterFtp)
                    domains = domains.Where(s => s.Servers.Any(x => x.Test.FeatureSet.SupportsTcpFtp));

                if (filterRsync)
                    domains = domains.Where(s => s.Servers.Any(x => x.Test.FeatureSet.SupportsTcpRsync));

                return domains.OrderBy(s => s.Domain).ToList();
            }
        }

        public static TldDomain GetSingleTld(string domain)
        {
            using (DnsDb db = new DnsDb())
                return db.Domains.Include(s => s.Servers).SingleOrDefault(s => s.Domain == domain);
        }

        public static List<TldServer> GetServersByIp(string ip)
        {
            using (DnsDb db = new DnsDb())
                return db.Servers.Where(s => s.ServerIp == ip).ToList();
        }

        public static List<DataServerListItem> GetAllServers()
        {
            using (DnsDb db = new DnsDb())
                return db.Servers.GroupBy(s => s.ServerIp)
                        .Select(s => new
                        {
                            Ip = s.Key,
                            Names = s.Select(x => x.ServerName),
                            HasIpv4 = s.Any(x => x.ServerType == ServerType.IPv4),
                            HasIpv6 = s.Any(x => x.ServerType == ServerType.IPv6),
                            Features = s.Select(x => x.Test.FeatureSet),
                            DomainCount = s.Count()
                        })
                        .AsEnumerable()
                        .Select(s => new DataServerListItem
                        {
                            Ip = s.Ip,
                            Name = s.Names.First(),
                            SupportsIpv4 = s.HasIpv4,
                            SupportsIpv6 = s.HasIpv6,
                            DomainCount = s.DomainCount,
                            FeatureSet = s.Features.Aggregate((set, featureSet) => set.Combine(featureSet))
                        }).ToList();
        }
    }
}