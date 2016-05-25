namespace WebShared.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddDomainScraping : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Domains",
                c => new
                    {
                        Fqdn = c.String(nullable: false, maxLength: 255),
                        ParentFqdn = c.String(nullable: false, maxLength: 255),
                        LastSeenUtc = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Fqdn);
            
            CreateTable(
                "dbo.DomainIps",
                c => new
                    {
                        Fqdn = c.String(nullable: false, maxLength: 255),
                        Ip = c.String(nullable: false, maxLength: 64),
                        LastSeenUtc = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => new { t.Fqdn, t.Ip })
                .ForeignKey("dbo.Domains", t => t.Fqdn, cascadeDelete: true)
                .Index(t => t.Fqdn);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.DomainIps", "Fqdn", "dbo.Domains");
            DropIndex("dbo.DomainIps", new[] { "Fqdn" });
            DropTable("dbo.DomainIps");
            DropTable("dbo.Domains");
        }
    }
}
