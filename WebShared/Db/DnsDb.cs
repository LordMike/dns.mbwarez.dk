using System.Data.Entity;
using WebShared.Migrations;

namespace WebShared.Db
{
    public class DnsDb : DbContext
    {
        public DbSet<TldDomain> Domains { get; set; }

        public DbSet<TldServer> Servers { get; set; }

        public DbSet<IpInfo> IpInfos { get; set; }

        public DbSet<Domain> ScrapedDomains { get; set; }

        public DbSet<DomainScrapeHistory> ScrapedDomainsHistory { get; set; }

        static DnsDb()
        {
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<DnsDb, Configuration>());
        }
    }
}
