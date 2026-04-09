using System;

namespace Miloooo.WCDB
{
    public enum WcdbValueKind
    {
        Null = 0,
        Int64 = 1,
        Double = 2,
        Text = 3,
        Blob = 4,
        Bool = 5,
    }

    public readonly struct WcdbValue
    {
        private readonly object? _value;

        private WcdbValue(WcdbValueKind kind, object? value)
        {
            Kind = kind;
            _value = value;
        }

        public WcdbValueKind Kind { get; }

        public object? BoxedValue => _value;

        public long AsInt64() => Convert.ToInt64(_value);

        public double AsDouble() => Convert.ToDouble(_value);

        public string? AsText() => _value as string;

        public byte[]? AsBlob() => _value as byte[];

        public bool AsBoolean() => _value is bool flag && flag;

        public static WcdbValue Null => new(WcdbValueKind.Null, null);

        public static WcdbValue FromObject(object? value)
        {
            if (value == null) {
                return Null;
            }

            if (value is WcdbValue wcdbValue) {
                return wcdbValue;
            }

            if (value is bool boolValue) {
                return new WcdbValue(WcdbValueKind.Bool, boolValue);
            }

            if (value is byte[] blobValue) {
                return new WcdbValue(WcdbValueKind.Blob, blobValue);
            }

            if (value is string textValue) {
                return new WcdbValue(WcdbValueKind.Text, textValue);
            }

            if (value is float or double or decimal) {
                return new WcdbValue(WcdbValueKind.Double, Convert.ToDouble(value));
            }

            if (value is sbyte or byte or short or ushort or int or uint or long or ulong) {
                return new WcdbValue(WcdbValueKind.Int64, Convert.ToInt64(value));
            }

            throw new NotSupportedException($"Unsupported WCDB value type: {value.GetType().FullName}");
        }

        public override string ToString()
        {
            return _value?.ToString() ?? "null";
        }

        public static implicit operator WcdbValue(string value) => new(WcdbValueKind.Text, value);

        public static implicit operator WcdbValue(long value) => new(WcdbValueKind.Int64, value);

        public static implicit operator WcdbValue(int value) => new(WcdbValueKind.Int64, (long) value);

        public static implicit operator WcdbValue(short value) => new(WcdbValueKind.Int64, (long) value);

        public static implicit operator WcdbValue(byte value) => new(WcdbValueKind.Int64, (long) value);

        public static implicit operator WcdbValue(bool value) => new(WcdbValueKind.Bool, value);

        public static implicit operator WcdbValue(double value) => new(WcdbValueKind.Double, value);

        public static implicit operator WcdbValue(float value) => new(WcdbValueKind.Double, (double) value);

        public static implicit operator WcdbValue(byte[] value) => new(WcdbValueKind.Blob, value);
    }
}
