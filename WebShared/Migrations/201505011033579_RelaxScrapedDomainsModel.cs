namespace WebShared.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RelaxScrapedDomainsModel : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Domains", "LastNameservers", c => c.String(maxLength: 1024));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Domains", "LastNameservers", c => c.String(nullable: false, maxLength: 1024));
        }
    }
}
