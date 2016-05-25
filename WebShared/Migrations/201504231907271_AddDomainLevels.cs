namespace WebShared.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddDomainLevels : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.TldDomains", "DomainLevel", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.TldDomains", "DomainLevel");
        }
    }
}
