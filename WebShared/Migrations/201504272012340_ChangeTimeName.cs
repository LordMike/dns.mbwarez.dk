namespace WebShared.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class ChangeTimeName : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.DomainScrapeHistories", "ScanTimeUtc", c => c.DateTime(nullable: false));
            DropColumn("dbo.DomainScrapeHistories", "LastScanUtc");
        }
        
        public override void Down()
        {
            AddColumn("dbo.DomainScrapeHistories", "LastScanUtc", c => c.DateTime(nullable: false));
            DropColumn("dbo.DomainScrapeHistories", "ScanTimeUtc");
        }
    }
}
