using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebShared.Db
{
    [ComplexType]
    public class ServerTest
    {
        public DateTime LastCheckUtc { get; set; }

        public ServerFeatureSet FeatureSet { get; set; }

        public ServerTest()
        {
            LastCheckUtc = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        }
    }
}