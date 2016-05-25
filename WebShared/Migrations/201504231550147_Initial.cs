namespace WebShared.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Initial : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.TldDomains",
                c => new
                    {
                        Domain = c.String(nullable: false, maxLength: 255),
                        ParentTld = c.String(maxLength: 255),
                        IsActive = c.Boolean(nullable: false),
                        CreatedTime = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Domain);
            
            CreateTable(
                "dbo.TldServers",
                c => new
                    {
                        Domain = c.String(nullable: false, maxLength: 255),
                        ServerIp = c.String(nullable: false, maxLength: 64),
                        CreatedTime = c.DateTime(nullable: false),
                        ServerName = c.String(nullable: false, maxLength: 255),
                        Refresh_LastCheckUtc = c.DateTime(nullable: false),
                        Refresh_LastCheckSuccess = c.Boolean(nullable: false),
                        Refresh_MasterServerDnsName = c.String(maxLength: 255),
                        Refresh_MasterResponsibleName = c.String(maxLength: 255),
                        Test_LastCheckUtc = c.DateTime(nullable: false),
                        Test_SupportsTcp = c.Boolean(nullable: false),
                        Test_SupportsUdp = c.Boolean(nullable: false),
                        Test_SupportsAxfr = c.Boolean(nullable: false),
                        Test_SupportsNsec = c.Boolean(nullable: false),
                        Test_SupportsNsec3 = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => new { t.Domain, t.ServerIp })
                .ForeignKey("dbo.TldDomains", t => t.Domain, cascadeDelete: true)
                .Index(t => t.Domain);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.TldServers", "Domain", "dbo.TldDomains");
            DropIndex("dbo.TldServers", new[] { "Domain" });
            DropTable("dbo.TldServers");
            DropTable("dbo.TldDomains");
        }
    }
}
