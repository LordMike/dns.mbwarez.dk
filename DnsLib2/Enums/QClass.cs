using System;

namespace DnsLib2.Enums
{
    public enum QClass : ushort
    {
        None = 0,
        IN = 1,
        [Obsolete("the CSNET class (Obsolete - used only for examples in some obsolete RFCs)")]
        CS = 2,
        CH = 3,
        HS = 4,
        ANY = 255,
    }
}