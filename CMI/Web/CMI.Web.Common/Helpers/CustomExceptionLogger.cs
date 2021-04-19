using System;
using System.Web.Http.ExceptionHandling;

namespace CMI.Web.Common.Helpers
{
    public class CustomExceptionLogger : ExceptionLogger
    {
        public override void Log(ExceptionLoggerContext context)
        {
            var id = Guid.NewGuid().ToString("N");
            context.Exception.Data.Add("EXCEPTIONID", id);
            Serilog.Log.Error(context.Exception, "Unhandled Exception {EXCEPTIONID}", id);
        }
    }
}