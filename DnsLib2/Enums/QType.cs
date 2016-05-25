using System;

namespace DnsLib2.Enums
{
    public enum QType : ushort
    {
        None = 0,
        A = 1,
        NS = 2,
        [Obsolete("No longer used. Provided for legacy purposes only.")]
        MD = 3,
        [Obsolete("No longer used. Provided for legacy purposes only.")]
        MF = 4,
        CNAME = 5,
        SOA = 6,
        MB = 7,
        MG = 8,
        MR = 9,
        NULL = 10,
        WKS = 11,
        PTR = 12,
        HINFO = 13,
        MINFO = 14,
        MX = 15,
        TXT = 16,
        RP = 17,
        AFSDB = 18,
        X25 = 19,
        ISDN = 20,
        RT = 21,
        NSAP = 22,
        [Obsolete("No longer used. Provided for legacy purposes only.")]
        NSAP_PTR = 23,
        SIG = 24,
        KEY = 25,
        PX = 26,
        [Obsolete("No longer used. Provided for legacy purposes only.")]
        GPOS = 27,
        AAAA = 28,
        LOC = 29,
        [Obsolete("No longer used. Provided for legacy purposes only.")]
        NXT = 30,
        EID = 31,
        NIMLOC = 32,
        SRV = 33,
        ATMA = 34,
        NAPTR = 35,
        KX = 36,
        CERT = 37,
        A6 = 38,
        DNAME = 39,
        SINK = 40,
        OPT = 41,
        APL = 42,
        DS = 43,
        SSHFP = 44,
        IPSECKEY = 45,
        RRSIG = 46,
        NSEC = 47,
        DNSKEY = 48,
        DHCID = 49,
        NSEC3 = 50,
        NSEC3PARAM = 51,
        HIP = 55,
        SPF = 99,
        UINFO = 100,
        UID = 101,
        GID = 102,
        UNSPEC = 103,
        TKEY = 249,
        TSIG = 250,
        IXFR = 251,
        AXFR = 252,
        MAILB = 253,
        [Obsolete("No longer used. Provided for legacy purposes only.")]
        MAILA = 254,
        ANY = 255,
        TA = 32768,
        DLV = 32769,
    }
}