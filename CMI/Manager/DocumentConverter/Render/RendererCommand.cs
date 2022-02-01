using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CMI.Contract.DocumentConverter;

namespace CMI.Manager.DocumentConverter.Render
{
    public enum VideoQuality
    {
        Low,
        Medium,
        High,
        Default = Medium
    }

    public class RendererCommand
    {
        public FileInfo SourceFile { get; set; }
        public string Identifier { get; set; }
        public string InputExtension { get; set; }
        public string OutputExtension { get; set; }
        public KeyValuePair<string, string>[] Parameters { get; set; }
        public int ProcessId { get; set; }
        public DateTime ProcessStartedDateTime { get; set; }
        public string WorkingDir => SourceFile.DirectoryName;

        public FileInfo TargetFile { get; set; }

        public string VideoQuality { get; set; }
        public JobContext Context { get; set; }

        /// <summary>
        /// The Abbyy ocr profile to use to generate the text layer
        /// </summary>
        public string PdfTextLayerExtractionProfile { get; set; }

        public string GetParameter(string name, string defaultValue)
        {
            return Parameters
                .Where(kv => kv.Key.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                .Select(kv => kv.Value)
                .FirstOrDefault() ?? defaultValue;
        }
    }
}