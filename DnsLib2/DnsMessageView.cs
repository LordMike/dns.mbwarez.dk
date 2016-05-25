using System;
using System.Collections.Generic;
using DnsLib2.Records;

namespace DnsLib2
{
    public class DnsMessageView
    {
        private byte[] _data;
        private int _offset;
        private int[] _recordOffsets;

        public DnsHeaderView Header { get; private set; }

        public DnsMessageView(byte[] data, int offset)
        {
            _data = data;
            _offset = offset;

            Header = new DnsHeaderView(data, offset);

            // Calculate offsets
            int records = Header.QuestionCount + Header.AnswerCount + Header.AuthorityCount + Header.AdditionalCount;
            _recordOffsets = new int[records];

            int idx = 0;
            int tmpOffset = offset + 12;        // 12 is the # bytes in the DNS header
            for (int i = 0; i < Header.QuestionCount; i++)
            {
                _recordOffsets[idx++] = tmpOffset;

                int length = DnsQuestionView.GetRecordLength(data, tmpOffset);
                tmpOffset += length;
            }

            for (int i = 0; i < Header.AnswerCount; i++)
            {
                _recordOffsets[idx++] = tmpOffset;

                int length = DnsRecordView.GetRecordLength(data, tmpOffset);
                tmpOffset += length;
            }

            for (int i = 0; i < Header.AuthorityCount; i++)
            {
                _recordOffsets[idx++] = tmpOffset;

                int length = DnsRecordView.GetRecordLength(data, tmpOffset);
                tmpOffset += length;
            }

            for (int i = 0; i < Header.AdditionalCount; i++)
            {
                _recordOffsets[idx++] = tmpOffset;

                int length = DnsRecordView.GetRecordLength(data, tmpOffset);
                tmpOffset += length;
            }
        }

        public DnsQuestionView GetQuestion(int idx)
        {
            if (idx >= Header.QuestionCount)
                throw new ArgumentOutOfRangeException("Input was out of range"); //TODO: add parameter name

            return new DnsQuestionView(_data, _recordOffsets[idx]);
        }

        public DnsRecordView GetAnswer(int idx)
        {
            if (idx >= Header.AnswerCount)
                throw new ArgumentOutOfRangeException("Input was out of range");

            return DnsRecordView.CreateRecord(_data, _recordOffsets[idx + Header.QuestionCount]);
        }

        public DnsRecordView GetAuthority(int idx)
        {
            if (idx >= Header.AuthorityCount)
                throw new ArgumentOutOfRangeException("Input was out of range");

            return DnsRecordView.CreateRecord(_data, _recordOffsets[idx + Header.QuestionCount + Header.AnswerCount]);
        }

        public DnsRecordView GetAdditional(int idx)
        {
            if (idx >= Header.AdditionalCount)
                throw new ArgumentOutOfRangeException("Input was out of range");

            return DnsRecordView.CreateRecord(_data, _recordOffsets[idx + Header.QuestionCount + Header.AnswerCount + Header.AuthorityCount]);
        }

        public IEnumerable<DnsQuestionView> Questions()
        {
            for (int i = 0; i < Header.QuestionCount; i++)
                yield return GetQuestion(i);
        }

        public IEnumerable<DnsRecordView> Answers()
        {
            for (int i = 0; i < Header.AnswerCount; i++)
                yield return GetAnswer(i);
        }

        public IEnumerable<DnsRecordView> Authorities()
        {
            for (int i = 0; i < Header.AuthorityCount; i++)
                yield return GetAuthority(i);
        }

        public IEnumerable<DnsRecordView> Additionals()
        {
            for (int i = 0; i < Header.AdditionalCount; i++)
                yield return GetAdditional(i);
        }

        public IEnumerable<DnsRecordView> AllRecords()
        {
            foreach (DnsRecordView record in Answers())
                yield return record;

            foreach (DnsRecordView record in Authorities())
                yield return record;

            foreach (DnsRecordView record in Additionals())
                yield return record;
        }
    }
}
