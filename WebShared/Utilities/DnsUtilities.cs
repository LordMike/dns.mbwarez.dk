using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using DnsLib2;
using DnsLib2.Common;
using DnsLib2.Enums;
using DnsLib2.Records;

namespace WebShared.Utilities
{
    public static class DnsUtilities
    {
        public static BuildOptRecord EdnsRecord = new BuildOptRecord(4096, 0x00008000);

        public static List<DnsRecordView> DoAxfr(IPEndPoint dnsServer, string dnsName)
        {
            List<DnsRecordView> answers = new List<DnsRecordView>();

            using (TcpClient client = new TcpClient())
            {
                client.Connect(dnsServer);

                using (NetworkStream stream = client.GetStream())
                {
                    DnsMessageBuilder builder = new DnsMessageBuilder();
                    builder.SetQuestion(dnsName, QType.AXFR).SetOpCode(DnsOpcode.Query);

                    byte[] queryBytes = builder.Encode();

                    // Send request
                    stream.WriteByte((byte)((queryBytes.Length >> 8) & 0xFF));
                    stream.WriteByte((byte)(queryBytes.Length & 0xFF));
                    stream.Write(queryBytes, 0, queryBytes.Length);

                    int soaCount = 0;

                    // Get responses
                    while (true)
                    {
                        short respLength = (short)(stream.ReadByte() << 8 | stream.ReadByte());

                        if (respLength <= 0)
                            // No more data
                            break;

                        byte[] respData = new byte[respLength];
                        int read = 0;
                        do
                        {
                            read += stream.Read(respData, read, respLength - read);
                        } while (read != respLength);

                        DnsMessageView view = new DnsMessageView(respData, 0);

                        RecordViewSOA soa = null;
                        foreach (DnsRecordView record in view.Answers())
                        {
                            soa = record as RecordViewSOA ?? soa;
                            answers.Add(record);
                        }

                        bool isSoa = soa != null;

                        if (soaCount == 0 && !isSoa)
                            // Expect start-SOA
                            throw new Exception("Invalid SOA response");

                        if (soaCount == 1 && isSoa)
                            // Got end-SOA
                            break;

                        if (isSoa)
                            soaCount++;
                    }
                }
            }

            // Sum all responses
            return answers;
        }

        public static bool TestAxfr(IPEndPoint dnsServer, string dnsName)
        {
            try
            {
                using (TcpClient client = new TcpClient())
                {
                    client.Connect(dnsServer);

                    using (NetworkStream stream = client.GetStream())
                    {
                        DnsMessageBuilder builder = new DnsMessageBuilder();
                        builder.SetQuestion(dnsName, QType.AXFR).SetOpCode(DnsOpcode.Query);

                        byte[] queryBytes = builder.Encode();

                        // Send request
                        stream.WriteByte((byte)((queryBytes.Length >> 8) & 0xFF));
                        stream.WriteByte((byte)(queryBytes.Length & 0xFF));
                        stream.Write(queryBytes, 0, queryBytes.Length);

                        // Get responses
                        short respLength = (short)(stream.ReadByte() << 8 | stream.ReadByte());

                        if (respLength <= 0)
                            // No data
                            return false;

                        byte[] respData = new byte[respLength];
                        int read = 0;
                        do
                        {
                            read += stream.Read(respData, read, respLength - read);
                        } while (read != respLength);

                        DnsMessageView view = new DnsMessageView(respData, 0);
                        RecordViewSOA soa = view.Answers().OfType<RecordViewSOA>().FirstOrDefault();

                        bool isSoa = soa != null;

                        if (!isSoa)
                            // Expect start-SOA
                            return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("TestAXFR " + dnsName + " " + dnsServer + ": " + ex.Message);
                return false;
            }
        }

        public static bool TestNsec(UdpDnsClient udpClient, QType testRecordType, IPEndPoint dnsServer, string dnsName)
        {
            try
            {
                DnsMessageView resp = null;

                // Try three times
                for (int i = 0; i < 3; i++)
                {
                    // Query zone
                    DnsMessageBuilder builder = new DnsMessageBuilder()
                         .SetFlag(DnsFlags.RecursionDesired | DnsFlags.AuthenticData)
                         .SetOpCode(DnsOpcode.Query)
                         .SetQuestion(dnsName, testRecordType)
                         .AddAdditional(EdnsRecord);

                    Task<DnsMessageView> respTask = udpClient.Query(dnsServer, builder);

                    try
                    {
                        resp = respTask.Result;

                        break;
                    }
                    catch (AggregateException ex)
                    {
                        Exception inner = ex.InnerException;

                        if (inner is TaskCanceledException)
                        {
                            // Timeout
                            continue;
                        }

                        throw;
                    }
                }

                if (resp == null)
                    // Unsuccessful multiple times
                    return false;

                // Check authority and answers
                return resp.AllRecords().Any(s => s.QType == testRecordType);
            }
            catch (Exception)
            {
                // Bad response
                return false;
            }
        }

        public static RecordViewSOA FetchSoaFromAuthorative(UdpDnsClient udpClient, IPEndPoint authorativeServer, string dnsName)
        {
            try
            {
                DnsMessageView resp = null;

                // Try three times
                for (int i = 0; i < 3; i++)
                {
                    // Query zone
                    Task<DnsMessageView> respTask = udpClient.Query(authorativeServer, new FQDN(dnsName), QType.SOA, QClass.IN, DnsFlags.AuthenticData);

                    try
                    {
                        resp = respTask.Result;

                        break;
                    }
                    catch (AggregateException ex)
                    {
                        Exception inner = ex.InnerException;

                        if (inner is TaskCanceledException)
                        {
                            // Timeout
                            continue;
                        }

                        throw;
                    }
                }

                if (resp == null)
                {
                    // Unsuccessful multiple times
                    return null;
                }

                // Check answers
                try
                {
                    return resp.Answers().OfType<RecordViewSOA>().FirstOrDefault();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("FetchSoa, exception: " + ex.Message);
                }

                return null;
            }
            catch (Exception)
            {
                // Bad data
                return null;
            }
        }

        public static bool TestDnssec(UdpDnsClient udpClient, IPEndPoint dnsServer, string dnsName)
        {
            try
            {
                DnsMessageView resp = null;

                // Try three times
                for (int i = 0; i < 3; i++)
                {
                    // Query zone
                    Task<DnsMessageView> respTask = udpClient.Query(dnsServer, new FQDN(dnsName), QType.DS);

                    try
                    {
                        resp = respTask.Result;

                        break;
                    }
                    catch (AggregateException ex)
                    {
                        Exception inner = ex.InnerException;

                        if (inner is TaskCanceledException)
                        {
                            // Timeout
                            continue;
                        }

                        throw;
                    }
                }

                if (resp == null)
                    // Unsuccessful multiple times
                    return false;

                // Check answers
                try
                {
                    return resp.Answers().Any(s => s.QType == QType.DS);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("TestDnssec, exception: " + ex.Message);
                }

                return false;
            }
            catch (Exception)
            {
                // Bad data
                return false;
            }
        }

        public static bool TestUdp(UdpDnsClient udpClient, IPEndPoint server, string dnsName)
        {
            try
            {
                DnsMessageBuilder query = new DnsMessageBuilder();
                query.SetQuestion(dnsName).SetOpCode(DnsOpcode.Query);

                DnsMessageView result = udpClient.Query(server, query).Result;

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool TestTcp(IPEndPoint server, string dnsName)
        {
            try
            {
                DnsMessageBuilder query = new DnsMessageBuilder();
                query.SetQuestion(dnsName).SetOpCode(DnsOpcode.Query);

                using (TcpDnsClient client = new TcpDnsClient(server))
                {
                    DnsMessageView result = client.Query(query).Result;

                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool TestTcpPort(IPEndPoint server)
        {
            try
            {
                using (TcpClient client = new TcpClient())
                {
                    IAsyncResult connectHandle = client.BeginConnect(server.Address, server.Port, null, null);
                    bool connected = connectHandle.AsyncWaitHandle.WaitOne(5000);
                    client.EndConnect(connectHandle);

                    return connected;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}