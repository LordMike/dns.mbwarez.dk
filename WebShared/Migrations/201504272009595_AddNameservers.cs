namespace WebShared.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddNameservers : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Domains", "LastNameservers", c => c.String(nullable: false, maxLength: 1024));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Domains", "LastNameservers");
        }
    }
}
