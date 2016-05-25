using System.ComponentModel;

namespace WebShared.Db
{
    public enum TldType
    {
        [Description("Unknown")]
        Unknown,

        [Description("Root")]
        Root,

        [Description("Country-Code TLD")]
        CcTld,

        [Description("Generic TLD")]
        GTld
    }
}