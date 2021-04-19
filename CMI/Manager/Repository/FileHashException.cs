using System;

namespace CMI.Manager.Repository
{
    internal class FileHashException : Exception
    {
        public FileHashException(string message) : base(message)
        {
        }
    }
}