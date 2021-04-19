using System;

namespace CMI.Contract.Common
{
    public class AISDatabaseNotFoundOrNotRunningException : Exception
    {
        public AISDatabaseNotFoundOrNotRunningException()
        {
        }

        public AISDatabaseNotFoundOrNotRunningException(string message)
            : base(message)
        {
        }

        public AISDatabaseNotFoundOrNotRunningException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}