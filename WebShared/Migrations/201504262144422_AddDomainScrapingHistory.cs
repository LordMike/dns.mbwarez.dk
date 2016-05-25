namespace WebShared.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddDomainScrapingHistory : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.DomainScrapeHistories",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Fqdn = c.String(nullable: false, maxLength: 255),
                        LastScanUtc = c.DateTime(nullable: false),
                        ScanType = c.String(nullable: false, maxLength: 60),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.TldDomains", t => t.Fqdn, cascadeDelete: true)
                .Index(t => t.Fqdn);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.DomainScrapeHistories", "Fqdn", "dbo.TldDomains");
            DropIndex("dbo.DomainScrapeHistories", new[] { "Fqdn" });
            DropTable("dbo.DomainScrapeHistories");
        }
    }
}
