using System;

namespace CMI.Tools.JsonCombiner.Tests
{
    public class ResourceNotFoundException : Exception
    {
        public ResourceNotFoundException(string message) : base(message)
        {
        }
    }
}