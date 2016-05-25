using System;
using DnsLib2.Common;
using DnsLib2.Enums;

namespace DnsLib2
{
    public class DnsHeaderView
    {
        private readonly byte[] _data;
        private readonly int _offset;
        private DnsFlags _flags;

        public ushort Id
        {
            get { return DnsEncoder.DecodeUshort(_data, _offset); }
        }

        public bool QueryIsResponse
        {
            get { return _flags.HasFlag(DnsFlags.QueryIsResponse); }
        }

        public DnsOpcode OpCode
        {
            get
            {
                return (DnsOpcode)((((ushort)_flags) >> 11) & 0x0F);
            }
        }

        public DnsFlags AllFlags { get { return _flags; } }

        public bool IsAuthorativeAnswer
        {
            get { return _flags.HasFlag(DnsFlags.AuthorativeAnswer); }
        }

        public bool IsTruncated
        {
            get { return _flags.HasFlag(DnsFlags.TruncatedResponse); }
        }

        public bool RequestRecursion
        {
            get { return _flags.HasFlag(DnsFlags.RecursionDesired); }
        }

        public bool IsRecursionAvailable
        {
            get { return _flags.HasFlag(DnsFlags.RecursionAvailable); }
        }

        public ResponseCode ResponseCode { get { return (ResponseCode)((ushort)_flags & 0x0F); } }

        public ushort QuestionCount { get { return DnsEncoder.DecodeUshort(_data, _offset + 4); } }
        public ushort AnswerCount { get { return DnsEncoder.DecodeUshort(_data, _offset + 6); } }
        public ushort AuthorityCount { get { return DnsEncoder.DecodeUshort(_data, _offset + 8); } }
        public ushort AdditionalCount { get { return DnsEncoder.DecodeUshort(_data, _offset + 10); } }

        public DnsHeaderView(byte[] data, int offset)
        {
            _data = data;
            _offset = offset;

            // Verify length
            if (_data.Length - _offset < 12)
                throw new ArgumentException();

            _flags = (DnsFlags)DnsEncoder.DecodeUshort(_data, _offset + 2);
        }
    }
}