using System;

namespace CMI.Utilities.Logging.Configurator
{
    public class BadRequestException : Exception
    {
        public BadRequestException(string friendlyName) : base(friendlyName)
        {
        }
    }

    public class ForbiddenException : Exception
    {
        public ForbiddenException(string friendlyName) : base(friendlyName)
        {
        }
    }
}