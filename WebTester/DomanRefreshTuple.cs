namespace WebTester
{
    public class DomanRefreshTuple
    {
        public string MasterResponsibleName { get; }
        public string MasterServerDnsName { get; }
        public int SoaRefreshTime { get; }

        public DomanRefreshTuple(string masterResponsibleName, string masterServerDnsName, int soaRefreshTime)
        {
            MasterResponsibleName = masterResponsibleName;
            MasterServerDnsName = masterServerDnsName;
            SoaRefreshTime = soaRefreshTime;
        }
    }
}