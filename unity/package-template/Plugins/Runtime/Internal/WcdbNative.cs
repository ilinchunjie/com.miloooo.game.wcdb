using System;
using System.Runtime.InteropServices;

namespace Miloooo.WCDB.Internal
{
    internal static class WcdbNative
    {
#if UNITY_IOS && !UNITY_EDITOR
        private const string LibraryName = "__Internal";
#else
        private const string LibraryName = "wcdb";
#endif

        internal enum NativeStatus
        {
            Ok = 0,
            Error = 1,
            Row = 100,
            Done = 101,
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct NativeBlob
        {
            public IntPtr Bytes;
            public int Length;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct NativeValue
        {
            public int Type;
            public int BoolValue;
            public long Int64Value;
            public double DoubleValue;
            public IntPtr TextValue;
            public NativeBlob BlobValue;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct NativeColumnDef
        {
            public IntPtr Name;
            public IntPtr DeclaredType;
            public int Flags;
            public IntPtr DefaultValueSql;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct NativeQueryOptions
        {
            public IntPtr WhereSql;
            public IntPtr OrderBySql;
            public int Limit;
            public int Offset;
            public int Flags;
        }

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int wcdb_has_feature(int feature);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr wcdb_open(IntPtr path, int readOnly);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void wcdb_close(IntPtr database);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int wcdb_last_error_code(IntPtr database);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr wcdb_last_error_message(IntPtr database);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern long wcdb_last_insert_rowid(IntPtr database);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int wcdb_changes(IntPtr database);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern NativeStatus wcdb_config_cipher(IntPtr database,
                                                               IntPtr keyBytes,
                                                               int keyLength);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern NativeStatus wcdb_begin_transaction(IntPtr database);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern NativeStatus wcdb_commit_transaction(IntPtr database);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern NativeStatus wcdb_rollback_transaction(IntPtr database);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern NativeStatus wcdb_execute(IntPtr database,
                                                         IntPtr sql,
                                                         IntPtr parameters,
                                                         int parameterCount);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr wcdb_prepare(IntPtr database, IntPtr sql);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void wcdb_statement_finalize(IntPtr statement);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern NativeStatus wcdb_statement_bind_value(IntPtr statement,
                                                                      int index,
                                                                      ref NativeValue value);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void wcdb_statement_clear_bindings(IntPtr statement);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void wcdb_statement_reset(IntPtr statement);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int wcdb_statement_parameter_index(IntPtr statement,
                                                                  IntPtr parameterName);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int wcdb_statement_column_count(IntPtr statement);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr wcdb_statement_column_name(IntPtr statement, int index);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int wcdb_statement_column_type(IntPtr statement, int index);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern long wcdb_statement_column_int64(IntPtr statement, int index);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern double wcdb_statement_column_double(IntPtr statement, int index);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr wcdb_statement_column_text(IntPtr statement, int index);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr wcdb_statement_column_blob(IntPtr statement, int index);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int wcdb_statement_column_bytes(IntPtr statement, int index);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern NativeStatus wcdb_statement_step(IntPtr statement);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern NativeStatus wcdb_create_table(IntPtr database,
                                                              IntPtr tableName,
                                                              IntPtr columns,
                                                              int columnCount,
                                                              int ifNotExists);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern NativeStatus wcdb_insert(IntPtr database,
                                                        IntPtr tableName,
                                                        IntPtr columnNames,
                                                        IntPtr values,
                                                        int columnCount,
                                                        int orReplace);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern NativeStatus wcdb_insert_many(IntPtr database,
                                                             IntPtr tableName,
                                                             IntPtr columnNames,
                                                             int columnCount,
                                                             IntPtr values,
                                                             int rowCount,
                                                             int orReplace);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern NativeStatus wcdb_update(IntPtr database,
                                                        IntPtr tableName,
                                                        IntPtr columnNames,
                                                        IntPtr values,
                                                        int columnCount,
                                                        IntPtr whereSql,
                                                        IntPtr whereParameters,
                                                        int whereParameterCount);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern NativeStatus wcdb_delete(IntPtr database,
                                                        IntPtr tableName,
                                                        IntPtr whereSql,
                                                        IntPtr whereParameters,
                                                        int whereParameterCount);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr wcdb_query_begin(IntPtr database,
                                                       IntPtr tableName,
                                                       IntPtr selectedColumns,
                                                       int selectedColumnCount,
                                                       ref NativeQueryOptions options,
                                                       IntPtr whereParameters,
                                                       int whereParameterCount);
    }
}
