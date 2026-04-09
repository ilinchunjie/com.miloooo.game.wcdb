using System;
using System.Collections.Generic;
using System.Linq;
using Miloooo.WCDB.Internal;

namespace Miloooo.WCDB
{
    public sealed class WcdbTable
    {
        private readonly WcdbDatabase _database;

        internal WcdbTable(WcdbDatabase database, string tableName)
        {
            _database = database;
            TableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
        }

        public string TableName { get; }

        public void Create(TableSchema schema, bool ifNotExists = true)
        {
            if (schema == null) {
                throw new ArgumentNullException(nameof(schema));
            }
            if (!string.Equals(schema.Name, TableName, StringComparison.Ordinal)) {
                throw new ArgumentException("Schema name must match the target table name.", nameof(schema));
            }
            using var tableNameScope = new NativeStringScope(TableName);
            using var columnScope = new NativeColumnDefinitionArrayScope(schema.Columns);
            _database.ThrowIfError(WcdbNative.wcdb_create_table(_database.NativeHandle,
                                                                tableNameScope.Pointer,
                                                                columnScope.Pointer,
                                                                columnScope.Count,
                                                                ifNotExists ? 1 : 0));
        }

        public void Insert(RowData row, bool orReplace = false)
        {
            if (row == null) {
                throw new ArgumentNullException(nameof(row));
            }
            if (row.Count == 0) {
                throw new ArgumentException("RowData must contain at least one column.", nameof(row));
            }

            string[] columnNames = row.ColumnNames.ToArray();
            WcdbValue[] values = columnNames.Select(columnName => row[columnName]).ToArray();
            using var tableNameScope = new NativeStringScope(TableName);
            using var columnScope = new NativeStringArrayScope(columnNames);
            using var valueScope = new NativeValueArrayScope(values);
            _database.ThrowIfError(WcdbNative.wcdb_insert(_database.NativeHandle,
                                                          tableNameScope.Pointer,
                                                          columnScope.Pointer,
                                                          valueScope.Pointer,
                                                          columnScope.Count,
                                                          orReplace ? 1 : 0));
        }

        public void InsertMany(IReadOnlyList<RowData> rows, bool orReplace = false)
        {
            if (rows == null) {
                throw new ArgumentNullException(nameof(rows));
            }
            if (rows.Count == 0) {
                return;
            }

            string[] columnNames = rows[0].ColumnNames.ToArray();
            var values = new List<WcdbValue>(rows.Count * columnNames.Length);

            foreach (RowData row in rows) {
                foreach (string columnName in columnNames) {
                    if (!row.TryGetValue(columnName, out WcdbValue value)) {
                        throw new ArgumentException($"All rows must contain column '{columnName}'.", nameof(rows));
                    }
                    values.Add(value);
                }
            }

            using var tableNameScope = new NativeStringScope(TableName);
            using var columnScope = new NativeStringArrayScope(columnNames);
            using var valueScope = new NativeValueArrayScope(values);
            _database.ThrowIfError(WcdbNative.wcdb_insert_many(_database.NativeHandle,
                                                               tableNameScope.Pointer,
                                                               columnScope.Pointer,
                                                               columnScope.Count,
                                                               valueScope.Pointer,
                                                               rows.Count,
                                                               orReplace ? 1 : 0));
        }

        public void Update(RowData values, SqlCondition? where = null)
        {
            if (values == null) {
                throw new ArgumentNullException(nameof(values));
            }
            if (values.Count == 0) {
                throw new ArgumentException("Update values cannot be empty.", nameof(values));
            }

            string[] columnNames = values.ColumnNames.ToArray();
            WcdbValue[] rowValues = columnNames.Select(name => values[name]).ToArray();

            using var tableNameScope = new NativeStringScope(TableName);
            using var columnScope = new NativeStringArrayScope(columnNames);
            using var valueScope = new NativeValueArrayScope(rowValues);
            using var whereScope = new NativeStringScope(where?.Sql);
            using var whereValueScope = new NativeValueArrayScope(where?.Parameters);

            _database.ThrowIfError(WcdbNative.wcdb_update(_database.NativeHandle,
                                                          tableNameScope.Pointer,
                                                          columnScope.Pointer,
                                                          valueScope.Pointer,
                                                          columnScope.Count,
                                                          whereScope.Pointer,
                                                          whereValueScope.Pointer,
                                                          whereValueScope.Count));
        }

        public void Delete(SqlCondition? where = null)
        {
            using var tableNameScope = new NativeStringScope(TableName);
            using var whereScope = new NativeStringScope(where?.Sql);
            using var whereValueScope = new NativeValueArrayScope(where?.Parameters);

            _database.ThrowIfError(WcdbNative.wcdb_delete(_database.NativeHandle,
                                                          tableNameScope.Pointer,
                                                          whereScope.Pointer,
                                                          whereValueScope.Pointer,
                                                          whereValueScope.Count));
        }

        public WcdbQueryResult Query(QueryOptions? options = null)
        {
            using var tableNameScope = new NativeStringScope(TableName);
            string[]? selectedColumns = options?.SelectedColumns?.ToArray();
            using var selectedColumnsScope = new NativeStringArrayScope(selectedColumns);
            using var queryOptionsScope = new NativeQueryOptionsScope(options);
            using var whereValueScope = new NativeValueArrayScope(options?.Condition?.Parameters);
            WcdbNative.NativeQueryOptions nativeQueryOptions = queryOptionsScope.Value;

            IntPtr statementHandle = WcdbNative.wcdb_query_begin(_database.NativeHandle,
                                                                 tableNameScope.Pointer,
                                                                 selectedColumnsScope.Pointer,
                                                                 selectedColumnsScope.Count,
                                                                 ref nativeQueryOptions,
                                                                 whereValueScope.Pointer,
                                                                 whereValueScope.Count);
            if (statementHandle == IntPtr.Zero) {
                _database.ThrowLastError();
            }

            return new WcdbQueryResult(new WcdbPreparedStatement(_database, statementHandle));
        }

        public RowData? QueryFirst(QueryOptions? options = null)
        {
            using WcdbQueryResult result = Query(options);
            return result.ReadNext(out RowData row) ? row : null;
        }
    }
}
