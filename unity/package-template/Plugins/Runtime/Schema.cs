using System;
using System.Collections.Generic;

namespace Miloooo.WCDB
{
    public enum ColumnType
    {
        Integer,
        Real,
        Text,
        Blob,
        Numeric,
    }

    [Flags]
    public enum ColumnFlags
    {
        None = 0,
        PrimaryKey = 1 << 0,
        NotNull = 1 << 1,
        Unique = 1 << 2,
        AutoIncrement = 1 << 3,
    }

    public sealed class ColumnSchema
    {
        public ColumnSchema(string name, ColumnType type)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Type = type;
        }

        public string Name { get; }

        public ColumnType Type { get; }

        public ColumnFlags Flags { get; private set; }

        public string? DefaultValueSql { get; private set; }

        public ColumnSchema PrimaryKey()
        {
            Flags |= ColumnFlags.PrimaryKey;
            return this;
        }

        public ColumnSchema NotNull()
        {
            Flags |= ColumnFlags.NotNull;
            return this;
        }

        public ColumnSchema Unique()
        {
            Flags |= ColumnFlags.Unique;
            return this;
        }

        public ColumnSchema AutoIncrement()
        {
            Flags |= ColumnFlags.AutoIncrement;
            return this;
        }

        public ColumnSchema DefaultSql(string sql)
        {
            DefaultValueSql = sql;
            return this;
        }

        internal string ToDeclaredType()
        {
            return Type switch
            {
                ColumnType.Integer => "INTEGER",
                ColumnType.Real => "REAL",
                ColumnType.Text => "TEXT",
                ColumnType.Blob => "BLOB",
                ColumnType.Numeric => "NUMERIC",
                _ => throw new ArgumentOutOfRangeException(nameof(Type)),
            };
        }
    }

    public sealed class TableSchema
    {
        private readonly List<ColumnSchema> _columns = new();

        public TableSchema(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public string Name { get; }

        public IReadOnlyList<ColumnSchema> Columns => _columns;

        public TableSchema Column(string name,
                                  ColumnType type,
                                  bool isPrimaryKey = false,
                                  bool notNull = false,
                                  bool unique = false,
                                  bool autoIncrement = false,
                                  string? defaultValueSql = null)
        {
            var column = new ColumnSchema(name, type);
            if (isPrimaryKey) {
                column.PrimaryKey();
            }
            if (notNull) {
                column.NotNull();
            }
            if (unique) {
                column.Unique();
            }
            if (autoIncrement) {
                column.AutoIncrement();
            }
            if (!string.IsNullOrWhiteSpace(defaultValueSql)) {
                column.DefaultSql(defaultValueSql);
            }
            _columns.Add(column);
            return this;
        }

        public TableSchema Column(ColumnSchema column)
        {
            _columns.Add(column ?? throw new ArgumentNullException(nameof(column)));
            return this;
        }
    }
}
