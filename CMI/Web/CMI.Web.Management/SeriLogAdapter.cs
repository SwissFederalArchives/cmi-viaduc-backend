using System;
using Kentor.AuthServices;
using Serilog;

namespace CMI.Web.Management
{
    public class SeriLogAdapter : ILoggerAdapter
    {
        private readonly ILogger logger;

        public SeriLogAdapter(ILogger logger)
        {
            this.logger = logger;
        }

        public void WriteError(string message, Exception ex)
        {
            logger.Error(ex, message);
        }

        public void WriteInformation(string message)
        {
            logger.Information(message);
        }

        public void WriteVerbose(string message)
        {
            logger.Verbose(message);
        }
    }
}