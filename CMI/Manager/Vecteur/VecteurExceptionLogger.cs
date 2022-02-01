using System;
using System.Web.Http.ExceptionHandling;

namespace CMI.Manager.Vecteur
{
    public class VecteurExceptionLogger: ExceptionLogger
    {
        public override void Log(ExceptionLoggerContext context)
        {
            Serilog.Log.Error(context.Exception, "Unhandled Exception occurred: Message is {message}", context.Exception.Message);
        }
    }
}
