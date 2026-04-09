using System;
using System.Collections;
using System.Collections.Generic;

namespace com.miloooo.game.wcdb
{
    public sealed class WcdbQueryResult : IEnumerable<RowData>, IDisposable
    {
        private readonly WcdbPreparedStatement _statement;

        internal WcdbQueryResult(WcdbPreparedStatement statement)
        {
            _statement = statement;
        }

        public bool ReadNext(out RowData row)
        {
            if (_statement.Step()) {
                row = _statement.ReadCurrentRow();
                return true;
            }
            row = null!;
            return false;
        }

        public IEnumerator<RowData> GetEnumerator()
        {
            while (ReadNext(out RowData row)) {
                yield return row;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Dispose()
        {
            _statement.Dispose();
        }
    }
}
