using System;
using System.Collections;
using System.Collections.Generic;

namespace Miloooo.WCDB
{
    public sealed class RowData : IEnumerable<KeyValuePair<string, WcdbValue>>
    {
        private readonly Dictionary<string, WcdbValue> _values = new(StringComparer.Ordinal);

        public WcdbValue this[string columnName]
        {
            get => _values[columnName];
            set => _values[columnName] = value;
        }

        public int Count => _values.Count;

        public ICollection<string> ColumnNames => _values.Keys;

        public ICollection<WcdbValue> Values => _values.Values;

        public void Add(string columnName, WcdbValue value)
        {
            _values.Add(columnName, value);
        }

        public bool ContainsKey(string columnName)
        {
            return _values.ContainsKey(columnName);
        }

        public bool TryGetValue(string columnName, out WcdbValue value)
        {
            return _values.TryGetValue(columnName, out value);
        }

        public IEnumerator<KeyValuePair<string, WcdbValue>> GetEnumerator()
        {
            return _values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
