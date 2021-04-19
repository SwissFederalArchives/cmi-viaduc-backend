using System;

namespace CMI.Contract.Order
{
    public class OrderDatabaseNotFoundOrNotRunningException : Exception
    {
        public OrderDatabaseNotFoundOrNotRunningException()
        {
        }

        public OrderDatabaseNotFoundOrNotRunningException(string message)
            : base(message)
        {
        }

        public OrderDatabaseNotFoundOrNotRunningException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}