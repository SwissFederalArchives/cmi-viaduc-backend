using System;

namespace CMI.Tools.UrbanCode
{
    internal class DescriptionNotSetException : Exception
    {
        public DescriptionNotSetException(string message) : base(message)
        {
        }
    }
}