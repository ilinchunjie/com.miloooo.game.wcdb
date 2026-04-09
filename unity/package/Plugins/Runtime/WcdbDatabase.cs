using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using com.miloooo.game.wcdb.Internal;

namespace com.miloooo.game.wcdb
{
    public sealed class WcdbDatabaseOptions
    {
        public bool ReadOnly { get; set; }

        public bool EnableWal { get; set; } = true;

        public byte[]? EncryptionKey { get; set; }
    }

    public sealed class WcdbDatabase : IDisposable
    {
        private readonly string _path;
        private IntPtr _nativeHandle;
        private int _transactionDepth;

        public WcdbDatabase(string path)
        {
            _path = path ?? throw new ArgumentNullException(nameof(path));
        }

        public static WcdbDatabase Open(string path, WcdbDatabaseOptions? options = null)
        {
            var database = new WcdbDatabase(path);
            database.Open(options);
            return database;
        }

        public bool IsOpen => _nativeHandle != IntPtr.Zero;

        public bool IsInTransaction => _transactionDepth > 0;

        public void Open(WcdbDatabaseOptions? options = null)
        {
            if (IsOpen) {
                return;
            }

            options ??= new WcdbDatabaseOptions();
            using var pathScope = new NativeStringScope(_path);
            _nativeHandle = WcdbNative.wcdb_open(pathScope.Pointer, options.ReadOnly ? 1 : 0);
            EnsureOpened();

            if (options.EncryptionKey is { Length: > 0 }) {
                GCHandle pin = GCHandle.Alloc(options.EncryptionKey, GCHandleType.Pinned);
                try {
                    ThrowIfError(WcdbNative.wcdb_config_cipher(_nativeHandle,
                                                               pin.AddrOfPinnedObject(),
                                                               options.EncryptionKey.Length));
                } finally {
                    pin.Free();
                }
            }

            if (options.EnableWal && !options.ReadOnly) {
                Execute("PRAGMA journal_mode = WAL");
            }
        }

        public bool HasFeature(int feature)
        {
            return WcdbNative.wcdb_has_feature(feature) != 0;
        }

        public void Close()
        {
            if (_nativeHandle != IntPtr.Zero) {
                WcdbNative.wcdb_close(_nativeHandle);
                _nativeHandle = IntPtr.Zero;
                _transactionDepth = 0;
            }
        }

        public void Dispose()
        {
            Close();
            GC.SuppressFinalize(this);
        }

        public WcdbTable Table(string tableName)
        {
            EnsureOpen();
            return new WcdbTable(this, tableName);
        }

        public void BeginTransaction()
        {
            ThrowIfError(WcdbNative.wcdb_begin_transaction(GetRequiredHandle()));
            _transactionDepth++;
        }

        public void Commit()
        {
            ThrowIfError(WcdbNative.wcdb_commit_transaction(GetRequiredHandle()));
            _transactionDepth = Math.Max(0, _transactionDepth - 1);
        }

        public void Rollback()
        {
            ThrowIfError(WcdbNative.wcdb_rollback_transaction(GetRequiredHandle()));
            _transactionDepth = Math.Max(0, _transactionDepth - 1);
        }

        public void Execute(string sql, params object?[] parameters)
        {
            EnsureOpen();
            using var sqlScope = new NativeStringScope(sql);
            using var valuesScope = new NativeValueArrayScope(ToValues(parameters));
            ThrowIfError(WcdbNative.wcdb_execute(_nativeHandle, sqlScope.Pointer, valuesScope.Pointer, valuesScope.Count));
        }

        public WcdbQueryResult Query(string sql, params object?[] parameters)
        {
            EnsureOpen();
            WcdbPreparedStatement statement = Prepare(sql);
            if (parameters.Length > 0) {
                for (int index = 0; index < parameters.Length; ++index) {
                    statement.Bind(index + 1, WcdbValue.FromObject(parameters[index]));
                }
            }
            return new WcdbQueryResult(statement);
        }

        public WcdbPreparedStatement Prepare(string sql)
        {
            EnsureOpen();
            using var sqlScope = new NativeStringScope(sql);
            IntPtr statementHandle = WcdbNative.wcdb_prepare(_nativeHandle, sqlScope.Pointer);
            if (statementHandle == IntPtr.Zero) {
                ThrowLastError();
            }
            return new WcdbPreparedStatement(this, statementHandle);
        }

        public void RunMigrations(IEnumerable<MigrationScript> scripts)
        {
            EnsureOpen();
            int currentVersion = GetUserVersion();
            foreach (MigrationScript script in scripts.OrderBy(script => script.TargetVersion)) {
                if (script.TargetVersion <= currentVersion) {
                    continue;
                }

                BeginTransaction();
                try {
                    foreach (Action<WcdbDatabase> step in script.Steps) {
                        step(this);
                    }
                    SetUserVersion(script.TargetVersion);
                    Commit();
                    currentVersion = script.TargetVersion;
                } catch {
                    Rollback();
                    throw;
                }
            }
        }

        public long LastInsertRowId => WcdbNative.wcdb_last_insert_rowid(GetRequiredHandle());

        public int Changes => WcdbNative.wcdb_changes(GetRequiredHandle());

        internal IntPtr NativeHandle => GetRequiredHandle();

        internal void ThrowIfError(WcdbNative.NativeStatus status)
        {
            if (status == WcdbNative.NativeStatus.Ok
                || status == WcdbNative.NativeStatus.Row
                || status == WcdbNative.NativeStatus.Done) {
                return;
            }
            ThrowLastError();
        }

        internal void ThrowLastError()
        {
            int errorCode = WcdbNative.wcdb_last_error_code(_nativeHandle);
            IntPtr messagePointer = WcdbNative.wcdb_last_error_message(_nativeHandle);
            string message = Marshal.PtrToStringAnsi(messagePointer) ?? "Unknown WCDB error.";
            throw new WcdbException(message, errorCode);
        }

        private IntPtr GetRequiredHandle()
        {
            EnsureOpen();
            return _nativeHandle;
        }

        private void EnsureOpened()
        {
            if (_nativeHandle == IntPtr.Zero) {
                throw new WcdbException("Failed to open wcdb native handle.", -1);
            }
            int errorCode = WcdbNative.wcdb_last_error_code(_nativeHandle);
            if (errorCode != 0) {
                ThrowLastError();
            }
        }

        private void EnsureOpen()
        {
            if (!IsOpen) {
                throw new InvalidOperationException("Database is not open.");
            }
        }

        private int GetUserVersion()
        {
            using WcdbQueryResult result = Query("PRAGMA user_version");
            if (result.ReadNext(out RowData row) && row.TryGetValue("user_version", out WcdbValue value)) {
                return (int) value.AsInt64();
            }
            return 0;
        }

        private void SetUserVersion(int version)
        {
            Execute($"PRAGMA user_version = {version}");
        }

        private static IReadOnlyList<WcdbValue> ToValues(IReadOnlyList<object?> objects)
        {
            var values = new WcdbValue[objects.Count];
            for (int index = 0; index < objects.Count; ++index) {
                values[index] = WcdbValue.FromObject(objects[index]);
            }
            return values;
        }
    }
}
