namespace WebShared.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddFtpRsyncTesting : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.TldServers", "Test_FeatureSet_SupportsTcpFtp", c => c.Boolean(nullable: false));
            AddColumn("dbo.TldServers", "Test_FeatureSet_SupportsTcpRsync", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.TldServers", "Test_FeatureSet_SupportsTcpRsync");
            DropColumn("dbo.TldServers", "Test_FeatureSet_SupportsTcpFtp");
        }
    }
}
