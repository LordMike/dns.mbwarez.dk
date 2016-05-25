namespace WebShared.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddIpInfoes : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.IpInfoes",
                c => new
                    {
                        Ip = c.String(nullable: false, maxLength: 64),
                        LastUpdateUtc = c.DateTime(nullable: false),
                        Hostname = c.String(maxLength: 255),
                        Loc = c.String(maxLength: 255),
                        Org = c.String(maxLength: 255),
                        City = c.String(maxLength: 255),
                        Region = c.String(maxLength: 255),
                        Country = c.String(maxLength: 255),
                        Postal = c.String(maxLength: 255),
                        Phone = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Ip);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.IpInfoes");
        }
    }
}
