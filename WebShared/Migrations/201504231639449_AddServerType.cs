namespace WebShared.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddServerType : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.TldServers", "ServerType", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.TldServers", "ServerType");
        }
    }
}
