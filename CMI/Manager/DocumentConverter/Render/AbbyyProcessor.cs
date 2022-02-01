using System;
using System.Collections.Generic;
using System.IO;
using CMI.Manager.DocumentConverter.Abbyy;
using CMI.Manager.DocumentConverter.Extraction.Interfaces;
using Newtonsoft.Json;

namespace CMI.Manager.DocumentConverter.Render
{
    public class AbbyyProcessor : RenderProcessorBase, INeedsAbbyyInstallation
    {
        private readonly IAbbyyWorker abbyyWorker;

        public string PathToAbbyyFrEngineDll { get; set; }
        public bool PathToAbbyFrEngineDllHasBeenSet { get; set; }
        public string MissingAbbyyPathInstallationMessage { get; set; }
        public bool MissingAbbyyPathInstallationMessageHasBeenSet { get; set; }

        public AbbyyProcessor(IAbbyyWorker abbyyWorker) : base("pdf", new List<string> { "tif", "tiff", "pdf" })
        {
            this.abbyyWorker = abbyyWorker;
        }


        public override FileInfo Render(RendererCommand rendererCommand)
        {
            cmd = rendererCommand;

            var result = TransformFile();
            return result.TargetFile;
        }

        private TransformResult TransformFile()
        {
            CreateAndStoreTitle();
            CreateAndStoreTargetFilePath();
            RenameSourceFileAndStoreValues();

            cmd.ProcessStartedDateTime = DateTime.Now;
            using (var streamWriter = File.CreateText(Path.Combine(cmd.WorkingDir, "info.json")))
            {
                var serializer = new JsonSerializer();
                serializer.Serialize(streamWriter, cmd);
            }

            return abbyyWorker.TransformDocument(cmd.PdfTextLayerExtractionProfile, cmd.SourceFile, cmd.TargetFile, cmd.Context);
        }

        protected override FileInfo GetResult()
        {
            throw new NotImplementedException("Method not required in AbbyyProcessor");
        }
    }
}