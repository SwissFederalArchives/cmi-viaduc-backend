using System;
using Rebex;
using Serilog;

namespace CMI.Manager.Vecteur
{
    public class SerilogWriter : LogWriterBase
    {
        public override void Write(LogLevel level, Type objectType, int objectId, string area, string message)
        {
            if (level < Level)
            {
                return;
            }

            switch (level)
            {
                case LogLevel.Debug:
                    Log.Logger.Debug(area + ":" + message);
                    break;

                case LogLevel.Error:
                    Log.Logger.Error(area + ":" + message);
                    break;

                case LogLevel.Info:
                    Log.Logger.Information(area + ":" + message);
                    break;

                case LogLevel.Off:
                    break;
            }
        }
    }
}