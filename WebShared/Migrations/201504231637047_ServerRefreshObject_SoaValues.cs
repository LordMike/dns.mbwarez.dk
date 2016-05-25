namespace WebShared.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class ServerRefreshObject_SoaValues : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.TldServers", "Refresh_Serial", c => c.Long(nullable: false));
            AddColumn("dbo.TldServers", "Refresh_RefreshTime", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.TldServers", "Refresh_RefreshTime");
            DropColumn("dbo.TldServers", "Refresh_Serial");
        }
    }
}
