using System;

namespace DnsLib2.Enums
{
    /// <summary>
    /// Flags in the DNS header
    /// See http://www.rfc-archive.org/getrfc.php?rfc=1035 section 4.1.1 and http://www.rfc-archive.org/getrfc.php?rfc=4035 section 3
    /// </summary>
    [Flags]
    public enum DnsFlags : ushort
    {
        /// <summary>
        /// Placeholder
        /// </summary>
        NoFlags = 0x0000,

        /// <summary>
        /// This query is a response
        /// </summary>
        QueryIsResponse = 0x8000,

        /// <summary>
        /// Authoritative Answer
        /// </summary>
        AuthorativeAnswer = 0x400,

        /// <summary>
        /// Truncated Response
        /// </summary>
        TruncatedResponse = 0x200,

        /// <summary>
        /// Recursion Desired
        /// </summary>
        RecursionDesired = 0x100,
        
        /// <summary>
        /// Recursion Allowed
        /// </summary>
        RecursionAvailable = 0x80,

        /// <summary>
        /// Authentic Data. Used in DNSSEC
        /// </summary>
        AuthenticData = 0x20,

        /// <summary>
        /// Checking Disabled. Used in DNSSEC
        /// </summary>
        CheckingDisabled = 0x10
    }
}
