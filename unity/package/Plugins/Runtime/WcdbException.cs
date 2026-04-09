using System;

namespace com.miloooo.game.wcdb
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
