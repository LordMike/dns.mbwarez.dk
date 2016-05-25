using System;
using System.Diagnostics;
using System.Text;
using DnsLib2.Utilities;

namespace DnsLib2.Common
{
    public static class DnsEncoder
    {
        private static ObjectPool<StringBuilder> _stringBuilders;
        private const int MaxLabels = 63;

        static DnsEncoder()
        {
            _stringBuilders = new ObjectPool<StringBuilder>();
            _stringBuilders.OnReturn = OnStringbuilderReturn;
        }

        private static void OnStringbuilderReturn(StringBuilder builder)
        {
            builder.Clear();
        }

        public static ushort DecodeUshort(byte[] array, int index)
        {
            if (array == null)
                throw new ArgumentNullException("array");

            if (index < 0 || array.Length - index < 2)
                throw new ArgumentOutOfRangeException("index");

            ushort value = BitConverter.ToUInt16(array, index);
            if (BitConverter.IsLittleEndian)
                return DnsUtils.HostToNetworkOrder(value);

            return value;
        }

        public static uint DecodeUint(byte[] array, int index)
        {
            if (array == null)
                throw new ArgumentNullException("array");

            if (index < 0 || array.Length - index < 4)
                throw new ArgumentOutOfRangeException("index");

            uint value = BitConverter.ToUInt32(array, index);
            if (BitConverter.IsLittleEndian)
                return DnsUtils.HostToNetworkOrder(value);

            return value;
        }

        public static int GetFqdnLength(byte[] data, int offset)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            if (offset < 0 || offset >= data.Length)
                throw new ArgumentOutOfRangeException("offset");

            int nextOffset = offset;

            int labels = 0;
            while (true)
            {
                // Read length byte
                byte length = data[nextOffset];

                if (length == 0)
                    // Cancel
                    return nextOffset + 1 - offset;

                if ((length & 0xC0) == 0xC0)
                    // Is a reference to a previous string
                    // Just exit here
                    return nextOffset + 2 - offset;

                // Skip the length
                nextOffset += 1 + length;

                if (labels++ > MaxLabels)
                    throw new ArgumentException("Provided data led to an infinite loop. Stopped after " + labels + " labels");
            }
        }

        public static int DecodeFqdn(byte[] data, int offset, out FQDN fqdn)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            if (offset < 0 || offset >= data.Length)
                throw new ArgumentOutOfRangeException("offset");

            int nextOffset = offset;
            int readBytes = -1;

            StringBuilder sb = _stringBuilders.Get();

            try
            {
                int labels = 0;
                while (true)
                {
                    // Read length byte
                    byte length = data[nextOffset];

                    if (length == 0)
                    {
                        // Cancel
                        if (readBytes == -1)
                            readBytes = nextOffset + 1 - offset;
                        break;
                    }

                    if ((length & 0xC0) == 0xC0)
                    {
                        // Is a reference to a previous string
                        byte secondByte = data[nextOffset + 1];

                        // Store how far we got
                        if (readBytes == -1)
                            readBytes = nextOffset + 2 - offset;

                        // Set next offset
                        length = (byte)(length & 0x3F);
                        nextOffset = (ushort)((length << 8) | secondByte);

                        continue;
                    }

                    // Parse text of given length
                    sb.Append(Encoding.ASCII.GetString(data, nextOffset + 1, length));
                    sb.Append('.');

                    nextOffset += 1 + length;

                    if (labels++ > MaxLabels)
                        throw new ArgumentException("Provided data led to an infinite loop. Stopped after " + labels + " labels");
                }

                fqdn = new FQDN(sb.ToString());
                return readBytes;
            }
            finally
            {
                _stringBuilders.Return(sb);
            }
        }

        public static int Encode(ushort value, byte[] array, int index)
        {
            if (array == null)
                throw new ArgumentNullException("array");

            if (index < 0 || array.Length - index < 2)
                throw new ArgumentOutOfRangeException("index");

            array[index] = (byte)((value >> 8) & 0xFF);
            array[index + 1] = (byte)((value) & 0xFF);

            return 2;
        }

        public static int Encode(uint value, byte[] array, int index)
        {
            if (array == null)
                throw new ArgumentNullException("array");

            if (index < 0 || array.Length - index < 4)
                throw new ArgumentOutOfRangeException("index");

            array[index] = (byte)((value >> 24) & 0xFF);
            array[index + 1] = (byte)((value >> 16) & 0xFF);
            array[index + 2] = (byte)((value >> 8) & 0xFF);
            array[index + 3] = (byte)((value) & 0xFF);

            return 4;
        }

        public static int Encode(FQDN value, byte[] array, int index)
        {
            // TODO: Value could be ref
            // TODO: We make sub-optimal strings, that don't backreference. This is an optimization though.

            if (array == null)
                throw new ArgumentNullException("array");

            if (index < 0 || array.Length - index < GetEncodedLength(value))
                throw new ArgumentOutOfRangeException("index");

            if (value.Name.Length <= 0)
            {
                Debug.Assert(false);        // TODO: Should not happen
                // Write out a 1-label FQDN with length 0.
                array[index] = 0;
                return 1;
            }

            int idx = index;
            string dnsName = value.Name;
            for (int i = 0; i <= dnsName.Length; i++)
            {
                // Calculate length
                int nextStop = dnsName.IndexOf('.', i);
                if (nextStop == -1)
                    nextStop = dnsName.Length;

                // Put in length
                int length = nextStop - i;
                array[idx++] = (byte)length;

                // Put in string
                for (int j = i; j < nextStop; j++)
                    array[idx++] = (byte)dnsName[j];

                i += length;
            }

            if (dnsName.Length == 1)
                // Special case for "."
                return 1;

            // Name already includes .'s: "www.XYZ.com."
            // Add 1 for initial length
            return dnsName.Length + 1;
        }

        public static int GetEncodedLength(FQDN value)
        {
            //TODO: value could be ref

            if (value.Name.Length == 1)
                // Special case for "."
                return 1;

            if (value.Name.Length <= 0)
            {
                Debug.Assert(false);        // TODO: Should not happen
                // DnsName is blank, so we encode this as 1 null-byte
                return 1;
            }

            // Name already includes .'s: "www.XYZ.com."
            // Add 1 for initial length
            return value.Name.Length + 1;
        }
    }
}