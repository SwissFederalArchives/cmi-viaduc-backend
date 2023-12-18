using CMI.Contract.Common;
using Serilog;
using System.Collections.Generic;
using System.IO;
using CMI.Engine.Asset.ParameterSettings;
using CMI.Engine.Asset.PreProcess;

namespace CMI.Engine.Asset.PostProcess
{
    public class PostProcessJp2Converter : ProcessAnalyzerBase
    {
        private readonly ViewerConversionSettings viewerSettings;
        private readonly ImageHelper imageHelper;

        public PostProcessJp2Converter(ViewerConversionSettings viewerSettings, ImageHelper imageHelper)
        {
            this.viewerSettings = viewerSettings;
            this.imageHelper = imageHelper;
        }


        protected override void AnalyzeFiles(string rootOrSubFolder, List<RepositoryFile> files)
        {
            foreach (var file in files)
            {
                var sourceFile = new FileInfo(Path.Combine(rootOrSubFolder, file.PhysicalName));
                if (sourceFile.Exists)
                {
                    ConvertToJpeg(sourceFile);
                }
            }
        }

        private void ConvertToJpeg(FileInfo sourceFile)
        {
            switch (sourceFile.Extension.ToLower())
            {
                case ".jp2":
                case ".tif":
                case ".tiff":
                    Log.Debug("Convert file {FullName} to jpg", sourceFile.FullName);
                    imageHelper.ConvertToJpeg(sourceFile.FullName, 100, this.viewerSettings.JpegQualitaetInProzent);
                    break;
            }
        }
    }
}
