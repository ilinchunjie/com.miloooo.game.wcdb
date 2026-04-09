using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Miloooo.WCDB.Internal
{
    internal sealed class NativeStringScope : IDisposable
    {
        public NativeStringScope(string? value)
        {
            Pointer = string.IsNullOrEmpty(value) ? IntPtr.Zero : Marshal.StringToHGlobalAnsi(value);
        }

        public IntPtr Pointer { get; }

        public void Dispose()
        {
            if (Pointer != IntPtr.Zero) {
                Marshal.FreeHGlobal(Pointer);
            }
        }
    }

    internal sealed class NativeStringArrayScope : IDisposable
    {
        private readonly List<IntPtr> _stringPointers = new();

        public NativeStringArrayScope(IReadOnlyList<string>? values)
        {
            Count = values?.Count ?? 0;
            if (Count == 0) {
                Pointer = IntPtr.Zero;
                return;
            }

            Pointer = Marshal.AllocHGlobal(IntPtr.Size * Count);
            for (int index = 0; index < Count; ++index) {
                IntPtr stringPointer = Marshal.StringToHGlobalAnsi(values![index]);
                _stringPointers.Add(stringPointer);
                Marshal.WriteIntPtr(Pointer, index * IntPtr.Size, stringPointer);
            }
        }

        public IntPtr Pointer { get; }

        public int Count { get; }

        public void Dispose()
        {
            foreach (IntPtr pointer in _stringPointers) {
                Marshal.FreeHGlobal(pointer);
            }
            if (Pointer != IntPtr.Zero) {
                Marshal.FreeHGlobal(Pointer);
            }
        }
    }

    internal sealed class NativeValueArrayScope : IDisposable
    {
        private readonly List<IntPtr> _allocations = new();
        private readonly int _elementSize;

        public NativeValueArrayScope(IReadOnlyList<WcdbValue>? values)
        {
            Count = values?.Count ?? 0;
            _elementSize = Marshal.SizeOf<WcdbNative.NativeValue>();
            if (Count == 0) {
                Pointer = IntPtr.Zero;
                return;
            }

            Pointer = Marshal.AllocHGlobal(_elementSize * Count);
            for (int index = 0; index < Count; ++index) {
                var nativeValue = ConvertValue(values![index]);
                Marshal.StructureToPtr(nativeValue, IntPtr.Add(Pointer, index * _elementSize), false);
            }
        }

        public IntPtr Pointer { get; }

        public int Count { get; }

        public void Dispose()
        {
            foreach (IntPtr allocation in _allocations) {
                Marshal.FreeHGlobal(allocation);
            }
            if (Pointer != IntPtr.Zero) {
                Marshal.FreeHGlobal(Pointer);
            }
        }

        private WcdbNative.NativeValue ConvertValue(WcdbValue value)
        {
            var nativeValue = new WcdbNative.NativeValue
            {
                Type = (int) value.Kind,
            };

            switch (value.Kind) {
            case WcdbValueKind.Null:
                break;
            case WcdbValueKind.Int64:
                nativeValue.Int64Value = value.AsInt64();
                break;
            case WcdbValueKind.Double:
                nativeValue.DoubleValue = value.AsDouble();
                break;
            case WcdbValueKind.Text:
                nativeValue.TextValue = Marshal.StringToHGlobalAnsi(value.AsText());
                _allocations.Add(nativeValue.TextValue);
                break;
            case WcdbValueKind.Blob:
                byte[]? blob = value.AsBlob();
                if (blob != null && blob.Length > 0) {
                    nativeValue.BlobValue.Length = blob.Length;
                    nativeValue.BlobValue.Bytes = Marshal.AllocHGlobal(blob.Length);
                    Marshal.Copy(blob, 0, nativeValue.BlobValue.Bytes, blob.Length);
                    _allocations.Add(nativeValue.BlobValue.Bytes);
                }
                break;
            case WcdbValueKind.Bool:
                nativeValue.BoolValue = value.AsBoolean() ? 1 : 0;
                break;
            default:
                throw new ArgumentOutOfRangeException();
            }

            return nativeValue;
        }
    }

    internal sealed class NativeColumnDefinitionArrayScope : IDisposable
    {
        private readonly List<IntPtr> _allocations = new();
        private readonly int _elementSize;

        public NativeColumnDefinitionArrayScope(IReadOnlyList<ColumnSchema> columns)
        {
            Count = columns.Count;
            _elementSize = Marshal.SizeOf<WcdbNative.NativeColumnDef>();
            if (Count == 0) {
                Pointer = IntPtr.Zero;
                return;
            }

            Pointer = Marshal.AllocHGlobal(_elementSize * Count);
            for (int index = 0; index < Count; ++index) {
                WcdbNative.NativeColumnDef nativeColumn = new()
                {
                    Name = Marshal.StringToHGlobalAnsi(columns[index].Name),
                    DeclaredType = Marshal.StringToHGlobalAnsi(columns[index].ToDeclaredType()),
                    Flags = (int) columns[index].Flags,
                    DefaultValueSql = string.IsNullOrWhiteSpace(columns[index].DefaultValueSql)
                        ? IntPtr.Zero
                        : Marshal.StringToHGlobalAnsi(columns[index].DefaultValueSql),
                };
                _allocations.Add(nativeColumn.Name);
                _allocations.Add(nativeColumn.DeclaredType);
                if (nativeColumn.DefaultValueSql != IntPtr.Zero) {
                    _allocations.Add(nativeColumn.DefaultValueSql);
                }
                Marshal.StructureToPtr(nativeColumn, IntPtr.Add(Pointer, index * _elementSize), false);
            }
        }

        public IntPtr Pointer { get; }

        public int Count { get; }

        public void Dispose()
        {
            foreach (IntPtr allocation in _allocations) {
                Marshal.FreeHGlobal(allocation);
            }
            if (Pointer != IntPtr.Zero) {
                Marshal.FreeHGlobal(Pointer);
            }
        }
    }

    internal sealed class NativeQueryOptionsScope : IDisposable
    {
        private readonly NativeStringScope _whereScope;
        private readonly NativeStringScope _orderByScope;

        public NativeQueryOptionsScope(QueryOptions? options)
        {
            _whereScope = new NativeStringScope(options?.Condition?.Sql);
            _orderByScope = new NativeStringScope(options?.OrderBySql);
            Value = new WcdbNative.NativeQueryOptions
            {
                WhereSql = _whereScope.Pointer,
                OrderBySql = _orderByScope.Pointer,
                Limit = options?.Limit ?? 0,
                Offset = options?.Offset ?? 0,
                Flags = (options?.Limit.HasValue ?? false ? 1 : 0)
                      | (options?.Offset.HasValue ?? false ? 2 : 0),
            };
        }

        public WcdbNative.NativeQueryOptions Value { get; }

        public void Dispose()
        {
            _whereScope.Dispose();
            _orderByScope.Dispose();
        }
    }
}
