using System;

namespace Miloooo.WCDB
{
    public sealed class WcdbException : Exception
    {
        public WcdbException(string message, int errorCode)
            : base(message)
        {
            ErrorCode = errorCode;
        }

        public int ErrorCode { get; }
    }
}
