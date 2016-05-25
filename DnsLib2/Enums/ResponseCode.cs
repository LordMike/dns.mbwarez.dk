namespace DnsLib2.Enums
{
    public enum ResponseCode : uint
    {
        NoError = 0,
        FormatError = 1,
        ServerFailure = 2,
        NegativeDomain = 3,

        Refused = 5,
        YXDomain = 6,
        YXRRSet = 7,
        NXRRSet = 8,
        NotAuthorative = 9,
        NotInZone = 10,
        //RESERVED11,
        //RESERVED12,
        //RESERVED13,
        //RESERVED14,
        //RESERVED15,
        //BADVERSSIG,
        //BADKEY,
        //BADTIME,
        //BADMODE,
        //BADNAME,
        //BADALG,
        //BADTRUNC,
    }
}