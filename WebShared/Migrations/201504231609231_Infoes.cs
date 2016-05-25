namespace WebShared.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Infoes : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.TldDomains", "Info_LastUpdateUtc", c => c.DateTime(nullable: false));
            AddColumn("dbo.TldDomains", "Info_Wikipage", c => c.String(maxLength: 255));
            AddColumn("dbo.TldDomains", "Info_Type", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.TldDomains", "Info_Type");
            DropColumn("dbo.TldDomains", "Info_Wikipage");
            DropColumn("dbo.TldDomains", "Info_LastUpdateUtc");
        }
    }
}
