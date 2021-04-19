using System;
using System.IO;
using System.Text;
using Serilog;

namespace CMI.Manager.DocumentConverter
{
    public class ConverterInstallationInfo
    {
        public ConverterInstallationInfo()
        {
            var processorExeFilesFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ProcessorExeFiles");
            PathToFfMpegExe = Path.Combine(processorExeFilesFolder, "ffmpeg.exe");
            PathToLameExe = Path.Combine(processorExeFilesFolder, "lame.exe");
            if (!TryVerifySettings(out var error))
            {
                Log.Error("Invalid settings for DocumentConverter.");
                throw new Exception($"Could not find one or more converters: {error}");
            }
        }

        /// <summary>
        ///     Gibt den Pfad inklusive Dateinamen zur Ffmpeg.exe an
        /// </summary>
        public string PathToFfMpegExe { get; set; }

        /// <summary>
        ///     Gibt den Pfad inklusive Dateinamen zur Lame.exe an
        /// </summary>
        public string PathToLameExe { get; set; }

        private bool TryVerifySettings(out string error)
        {
            error = "";
            var sb = new StringBuilder();

            if (!File.Exists(PathToFfMpegExe))
            {
                sb.AppendLine($"File '{PathToFfMpegExe}' not found");
            }

            if (!File.Exists(PathToLameExe))
            {
                sb.AppendLine($"File '{PathToLameExe}' not found");
            }

            if (sb.Length <= 0)
            {
                return true;
            }

            error = sb.ToString();
            return false;
        }
    }
}