using System;

namespace Function.Domain.Helpers
{
    public class CustomLogging : Exception
    {
        public int ErrorCode { get; }
        public string CustomMessage { get; }
        public object CustomData { get; }

        public CustomLogging(int errorCode, string message, string customMessage = null, object customData = null)
            : base(message)
        {
            ErrorCode = errorCode;
            CustomMessage = customMessage;
            CustomData = customData;
        }
    }
}