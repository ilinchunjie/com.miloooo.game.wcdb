using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Miloooo.WCDB.Internal;

namespace Miloooo.WCDB
{
    public sealed class WcdbPreparedStatement : IDisposable
    {
        private readonly WcdbDatabase _database;
        private IntPtr _nativeHandle;

        internal WcdbPreparedStatement(WcdbDatabase database, IntPtr nativeHandle)
        {
            _database = database;
            _nativeHandle = nativeHandle;
        }

        public int ColumnCount
        {
            get
            {
                EnsureNotDisposed();
                return WcdbNative.wcdb_statement_column_count(_nativeHandle);
            }
        }

        public void Bind(int index, WcdbValue value)
        {
            EnsureNotDisposed();
            using var values = new NativeValueArrayScope(new[] { value });
            var nativeValue = Marshal.PtrToStructure<WcdbNative.NativeValue>(values.Pointer);
            WcdbNative.NativeStatus status
                = WcdbNative.wcdb_statement_bind_value(_nativeHandle, index, ref nativeValue);
            _database.ThrowIfError(status);
        }

        public void Bind(string parameterName, WcdbValue value)
        {
            EnsureNotDisposed();
            using var nameScope = new NativeStringScope(parameterName);
            int index = WcdbNative.wcdb_statement_parameter_index(_nativeHandle, nameScope.Pointer);
            if (index <= 0) {
                throw new ArgumentException($"Parameter '{parameterName}' was not found.", nameof(parameterName));
            }
            Bind(index, value);
        }

        public void BindAll(IReadOnlyList<WcdbValue> values)
        {
            if (values == null) {
                throw new ArgumentNullException(nameof(values));
            }
            for (int index = 0; index < values.Count; ++index) {
                Bind(index + 1, values[index]);
            }
        }

        public bool Step()
        {
            EnsureNotDisposed();
            WcdbNative.NativeStatus status = WcdbNative.wcdb_statement_step(_nativeHandle);
            if (status == WcdbNative.NativeStatus.Row) {
                return true;
            }
            if (status == WcdbNative.NativeStatus.Done) {
                return false;
            }
            _database.ThrowIfError(status);
            return false;
        }

        public RowData ReadCurrentRow()
        {
            EnsureNotDisposed();
            var row = new RowData();
            int columnCount = ColumnCount;
            for (int index = 0; index < columnCount; ++index) {
                string? columnName
                    = Marshal.PtrToStringAnsi(WcdbNative.wcdb_statement_column_name(_nativeHandle, index));
                if (string.IsNullOrEmpty(columnName)) {
                    columnName = $"column_{index}";
                }
                row[columnName] = ReadColumn(index);
            }
            return row;
        }

        public void Reset()
        {
            EnsureNotDisposed();
            WcdbNative.wcdb_statement_reset(_nativeHandle);
            WcdbNative.wcdb_statement_clear_bindings(_nativeHandle);
        }

        public void Dispose()
        {
            if (_nativeHandle != IntPtr.Zero) {
                WcdbNative.wcdb_statement_finalize(_nativeHandle);
                _nativeHandle = IntPtr.Zero;
            }
        }

        internal IntPtr DangerousGetHandle()
        {
            return _nativeHandle;
        }

        internal WcdbValue ReadColumn(int index)
        {
            int type = WcdbNative.wcdb_statement_column_type(_nativeHandle, index);
            return type switch
            {
                (int) WcdbValueKind.Int64 => WcdbNative.wcdb_statement_column_int64(_nativeHandle, index),
                (int) WcdbValueKind.Double => WcdbNative.wcdb_statement_column_double(_nativeHandle, index),
                (int) WcdbValueKind.Text =>
                    WcdbValue.FromObject(Marshal.PtrToStringAnsi(
                        WcdbNative.wcdb_statement_column_text(_nativeHandle, index))),
                (int) WcdbValueKind.Blob => ReadBlob(index),
                _ => WcdbValue.Null,
            };
        }

        private WcdbValue ReadBlob(int index)
        {
            IntPtr blobPointer = WcdbNative.wcdb_statement_column_blob(_nativeHandle, index);
            int length = WcdbNative.wcdb_statement_column_bytes(_nativeHandle, index);
            if (blobPointer == IntPtr.Zero || length <= 0) {
                return WcdbValue.Null;
            }
            var buffer = new byte[length];
            Marshal.Copy(blobPointer, buffer, 0, length);
            return buffer;
        }

        private void EnsureNotDisposed()
        {
            if (_nativeHandle == IntPtr.Zero) {
                throw new ObjectDisposedException(nameof(WcdbPreparedStatement));
            }
        }
    }
}
