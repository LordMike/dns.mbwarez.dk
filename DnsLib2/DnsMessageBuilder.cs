using System.Collections.Generic;
using DnsLib2.Common;
using DnsLib2.Enums;
using DnsLib2.Records;

namespace DnsLib2
{
    public class DnsMessageBuilder
    {
        private FQDN _qName;
        private QType _qType;
        private QClass _qClass;

        private DnsFlags _flags;
        private DnsOpcode _opcode;
        private ResponseCode _responseCode;

        private ushort _id;

        private List<BuildRecordBase> _answers;
        private List<BuildRecordBase> _authorities;
        private List<BuildRecordBase> _additionals;

        public DnsMessageBuilder()
        {
            _answers = new List<BuildRecordBase>();
            _authorities = new List<BuildRecordBase>();
            _additionals = new List<BuildRecordBase>();
        }

        public DnsMessageBuilder SetId(ushort id)
        {
            _id = id;
            return this;
        }

        public DnsMessageBuilder SetFlag(DnsFlags flag)
        {
            _flags |= flag;
            return this;
        }

        public DnsMessageBuilder SetOpCode(DnsOpcode code)
        {
            _opcode = code;
            return this;
        }

        public DnsMessageBuilder SetResponseCode(ResponseCode code)
        {
            _responseCode = code;
            return this;
        }

        public DnsMessageBuilder SetQuestion(DnsQuestionView question)
        {
            _qName = question.QName;
            _qType = question.QType;
            _qClass = question.QClass;

            return this;
        }

        public DnsMessageBuilder SetQuestion(string name, QType type = QType.A, QClass @class = QClass.IN)
        {
            return SetQuestion(new FQDN(name), type, @class);
        }

        public DnsMessageBuilder SetQuestion(FQDN name, QType type = QType.A, QClass @class = QClass.IN)
        {
            _qName = name;
            _qType = type;
            _qClass = @class;

            return this;
        }

        public DnsMessageBuilder AddAnswer(BuildRecordBase record)
        {
            _answers.Add(record);
            return this;
        }

        public DnsMessageBuilder AddAuthority(BuildRecordBase record)
        {
            _authorities.Add(record);
            return this;
        }

        public DnsMessageBuilder AddAdditional(BuildRecordBase record)
        {
            _additionals.Add(record);
            return this;
        }

        public byte[] Encode()
        {
            int length = GetEncodedLength();
            byte[] buffer = new byte[length];
            Encode(buffer, 0);
            return buffer;
        }

        public int Encode(byte[] buffer, int offset)
        {
            //TODO: check buffer min size and offset should not be outside bounds
            int newOffset = offset;

            // Header
            newOffset += DnsEncoder.Encode(_id, buffer, newOffset);

            ushort flags = (ushort)_flags;
            flags += (ushort)_responseCode;

            newOffset += DnsEncoder.Encode(flags, buffer, newOffset);

            // Counts
            newOffset += DnsEncoder.Encode(1, buffer, newOffset);
            newOffset += DnsEncoder.Encode((ushort)_answers.Count, buffer, newOffset);
            newOffset += DnsEncoder.Encode((ushort)_authorities.Count, buffer, newOffset);
            newOffset += DnsEncoder.Encode((ushort)_additionals.Count, buffer, newOffset);

            // Encode question
            newOffset += DnsEncoder.Encode(_qName, buffer, newOffset);
            newOffset += DnsEncoder.Encode((ushort)_qType, buffer, newOffset);
            newOffset += DnsEncoder.Encode((ushort)_qClass, buffer, newOffset);

            // Encode answers, authorities and additionals
            foreach (BuildRecordBase record in _answers)
                newOffset += record.Encode(buffer, newOffset);

            foreach (BuildRecordBase record in _authorities)
                newOffset += record.Encode(buffer, newOffset);

            foreach (BuildRecordBase record in _additionals)
                newOffset += record.Encode(buffer, newOffset);

            return newOffset;
        }

        public int GetEncodedLength()
        {
            // Base
            int len = 12;

            // Question
            len += DnsEncoder.GetEncodedLength(_qName);
            len += 4;

            // Records
            foreach (BuildRecordBase record in _answers)
                len += record.GetEncodedLength();

            foreach (BuildRecordBase record in _authorities)
                len += record.GetEncodedLength();

            foreach (BuildRecordBase record in _additionals)
                len += record.GetEncodedLength();

            return len;
        }

        public void Clear()
        {
            _id = 0;

            _answers.Clear();
            _authorities.Clear();
            _additionals.Clear();

            _flags = 0;
            _opcode = 0;
            _responseCode = 0;
        }
    }
}