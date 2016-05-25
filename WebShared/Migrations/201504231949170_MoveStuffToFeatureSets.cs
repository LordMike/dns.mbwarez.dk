namespace WebShared.Migrations
{
    using System;
    using System.Data.Entity.Migrations;

    public partial class MoveStuffToFeatureSets : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.TldDomains", "MasterResponsibleName", c => c.String(maxLength: 255));
            AddColumn("dbo.TldDomains", "MasterServerDnsName", c => c.String(maxLength: 255));
            AddColumn("dbo.TldDomains", "SoaRefreshTime", c => c.Long(nullable: false));
            RenameColumn("dbo.TldServers", "Test_SupportsTcp", "Test_FeatureSet_SupportsTcp");
            RenameColumn("dbo.TldServers", "Test_SupportsUdp", "Test_FeatureSet_SupportsUdp");
            RenameColumn("dbo.TldServers", "Test_SupportsAxfr", "Test_FeatureSet_SupportsAxfr");
            RenameColumn("dbo.TldServers", "Test_SupportsNsec", "Test_FeatureSet_SupportsNsec");
            RenameColumn("dbo.TldServers", "Test_SupportsNsec3", "Test_FeatureSet_SupportsNsec3");
        }

        public override void Down()
        {
            RenameColumn("dbo.TldServers", "Test_FeatureSet_SupportsTcp", "Test_SupportsTcp");
            RenameColumn("dbo.TldServers", "Test_FeatureSet_SupportsUdp", "Test_SupportsUdp");
            RenameColumn("dbo.TldServers", "Test_FeatureSet_SupportsAxfr", "Test_SupportsAxfr");
            RenameColumn("dbo.TldServers", "Test_FeatureSet_SupportsNsec", "Test_SupportsNsec");
            RenameColumn("dbo.TldServers", "Test_FeatureSet_SupportsNsec3", "Test_SupportsNsec3");
            DropColumn("dbo.TldDomains", "SoaRefreshTime");
            DropColumn("dbo.TldDomains", "MasterServerDnsName");
            DropColumn("dbo.TldDomains", "MasterResponsibleName");
        }
    }
}
