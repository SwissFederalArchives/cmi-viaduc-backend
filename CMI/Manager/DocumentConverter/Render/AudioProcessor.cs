using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;

namespace CMI.Manager.DocumentConverter.Render
{
    public class AudioProcessor : RenderProcessorBase
    {
        public AudioProcessor() :
            base("mp3", new List<string> {"wav"})
        {
        }

        public override FileInfo Render(RendererCommand rendererCommand)
        {
            cmd = rendererCommand;

            var process = Start();

            process.WaitForExit();

            return GetResult();
        }

        private Process Start()
        {
            CreateAndStoreTitle();
            CreateAndStoreTargetFilePath();
            RenameSourceFileAndStoreValues();

            var process = new Process
            {
                StartInfo =
                {
                    WorkingDirectory = cmd.WorkingDir,
                    FileName = "cmd.exe", // damit möglichst einfach auf stderr zugegriffen werden kann
                    CreateNoWindow = true,
                    UseShellExecute = false
                }
            };

            // ToDo: Replace with configuration
            var comment = "Schweizerisches Bundesarchiv BAR";

            var lameCmd = $@"""{ProcessorPath}"" -b 128 ""{cmd.SourceFile.FullName}"" ""{cmd.TargetFile.FullName}"" -c -o";
            if (!string.IsNullOrEmpty(title))
            {
                lameCmd += $@" --tt ""{title}""";
            }

            if (!string.IsNullOrEmpty(comment))
            {
                lameCmd += $@" --tc ""{comment}""";
            }

            // lame schreibt in stderr und nicht in stdout, darum 2>
            process.StartInfo.Arguments = $@"/C ""{lameCmd}"" 2> {logFileName}";
            process.Start();

            cmd.ProcessStartedDateTime = DateTime.Now;
            cmd.ProcessId = process.Id;

            using (var streamWriter = File.CreateText(Path.Combine(cmd.WorkingDir, "info.json")))
            {
                var serializer = new JsonSerializer();
                serializer.Serialize(streamWriter, cmd);
            }

            return process;
        }

        protected override FileInfo GetResult()
        {
            cmd.TargetFile.Refresh();
            var logFile = GetLogFilePath();
            if (logFile.Exists)
            {
                var unsupportedAudioFormat = "unsupported audio format";

                // This could indicate that the data was compressed using Microsoft's ADPCM codec. 
                // LAME supports PCM only. 
                var unsupportedDataFormat = "unsupported data format";

                // Check the log file
                var lines = File.ReadAllLines(logFile.FullName);
                foreach (var line in lines)
                {
                    if (line == null)
                    {
                        break;
                    }

                    if (line.IndexOf("Writing LAME Tag...done", StringComparison.InvariantCultureIgnoreCase) >= 0)
                    {
                        return cmd.TargetFile;
                    }

                    if (line.IndexOf(unsupportedAudioFormat, StringComparison.InvariantCultureIgnoreCase) >= 0)
                    {
                        throw new InvalidOperationException(
                            $"The file {cmd.TargetFile.Name} could not be created. Error reason is: {unsupportedAudioFormat}.");
                    }

                    if (line.IndexOf(unsupportedDataFormat, StringComparison.CurrentCultureIgnoreCase) >= 0)
                    {
                        throw new InvalidOperationException(
                            $"The file {cmd.TargetFile.Name} could not be created. Error reason is: {unsupportedDataFormat}.");
                    }
                }
            }

            if (!cmd.TargetFile.Exists || cmd.TargetFile.Length == 0)
            {
                throw new InvalidOperationException(
                    $"The file {cmd.TargetFile.Name} could not be created. No indication for error found in log file or log file not created.");
            }

            // Even without logfile we produced a mp3 file
            return cmd.TargetFile;
        }
    }
}