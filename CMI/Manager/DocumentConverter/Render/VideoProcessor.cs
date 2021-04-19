using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;

namespace CMI.Manager.DocumentConverter.Render
{
    public class VideoProcessor : RenderProcessorBase
    {
        private readonly VideoQuality defaultVideoQuality = VideoQuality.Medium;
        private VideoQuality videoQuality = VideoQuality.Medium;

        public VideoProcessor()
            : base("mp4", new List<string> {"mp4"})
        {
        }

        public override FileInfo Render(RendererCommand rendererCommand)
        {
            cmd = rendererCommand;

            SetVideoQuality(rendererCommand.VideoQuality);

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

            var comment = "Schweizerisches Bundesarchiv BAR";

            // https://trac.ffmpeg.org/wiki/Scaling%20(resizing)%20with%20ffmpeg
            // TODO: Wäre schöner, wenn diese Einstellungen in den Parameters oder einer Config-Datei hinterlegt würden
            string[] scale = {"320:240", "720x480", "1280x760"};

            var procCmd = $@"""{ProcessorPath}"" -i ""{cmd.SourceFile}"" -vf scale={scale[(int) videoQuality]}";
            if (!string.IsNullOrEmpty(title))
            {
                procCmd += $@" -metadata title=""{title}""";
            }

            if (!string.IsNullOrEmpty(comment))
            {
                procCmd += $@" -metadata comment=""{comment}""";
            }

            procCmd += $@" ""{cmd.TargetFile}""";

            process.StartInfo.Arguments = $@"/C ""{procCmd}"" 2> log.txt";
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

            if (!cmd.TargetFile.Exists || cmd.TargetFile.Length == 0)
            {
                throw new ApplicationException($"keine {OutputExtension}-Datei gefunden");
            }

            var logFile = GetLogFilePath();

            if (!logFile.Exists)
            {
                return cmd.TargetFile;
            }

            using (var streamReader = File.OpenText(logFile.FullName))
            {
                while (true)
                {
                    var line = streamReader.ReadLine();
                    if (line == null)
                    {
                        break;
                    }

                    if (line.Contains("No such file or directory"))
                    {
                        throw new Exception(CreateTargetFileNotFoundMessage());
                    }

                    if (line.Contains("Invalid data found when processing input"))
                    {
                        throw new Exception(CreateTargetFileNotFoundMessage());
                    }
                }
            }

            return cmd.TargetFile;
        }

        private string CreateTargetFileNotFoundMessage()
        {
            return $"Can't find target file '{cmd.TargetFile.FullName}'";
        }

        private void SetVideoQuality(string desiredVideoQuality)
        {
            if (Enum.TryParse(desiredVideoQuality, true, out VideoQuality existingVideoQuality))
            {
                videoQuality = existingVideoQuality;
                return;
            }

            videoQuality = defaultVideoQuality;
        }
    }
}