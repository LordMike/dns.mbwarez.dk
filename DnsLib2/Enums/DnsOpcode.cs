namespace DnsLib2.Enums
{
    public enum DnsOpcode : byte
    {
        Query = 0,
        IQuery = 1,
        Status = 2,
        Notify = 4,
        Update = 5
    }
}