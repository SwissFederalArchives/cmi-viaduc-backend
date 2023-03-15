using CMI.Contract.Common;
using CMI.Engine.Asset.PreProcess;
using CSJ2K;
using Serilog;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using CMI.Engine.Asset.ParameterSettings;

namespace CMI.Engine.Asset.PostProcess
{
    public class PostProcessJp2Converter : ProcessAnalyzerBase
    {
        private readonly ViewerConversionSettings viewerSettings;
        private readonly EncoderParameters encoderParameters;
        private readonly ImageCodecInfo jpegEncoder;

        public PostProcessJp2Converter(ViewerConversionSettings viewerSettings)
        {
            this.viewerSettings = viewerSettings;
            encoderParameters = new EncoderParameters(1);
            var encoderParameter = new EncoderParameter(Encoder.Quality, this.viewerSettings.JpegQualitaetInProzent);
            encoderParameters.Param[0] = encoderParameter;
            jpegEncoder = ImageCodecInfo.GetImageEncoders().First(e => e.MimeType == "image/jpeg");
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
                {
                    Log.Information("Convert jp2 file {FullName} to jpg", sourceFile.FullName);
                    var decodedImage = J2kImage.FromFile(sourceFile.FullName);
                    using var jp2Bitmap = decodedImage.As<Bitmap>();
                    jp2Bitmap.SetResolution(viewerSettings.DefaultAufloesungInDpi, viewerSettings.DefaultAufloesungInDpi);
                    SaveAsJpeg(jp2Bitmap, Path.ChangeExtension(sourceFile.FullName, ".jpg"));
                    break;
                }
                case ".tif":
                case ".tiff":
                    Log.Information("Convert tiff file {FullName} to jpg", sourceFile.FullName);
                    var tiffBitmap = Image.FromFile(sourceFile.FullName);
                    SaveAsJpeg(tiffBitmap, Path.ChangeExtension(sourceFile.FullName, ".jpg"));
                    break;
            }
        }

        private void SaveAsJpeg(Image bitmap, string newFileName)
        {
            bitmap.Save(newFileName, jpegEncoder, encoderParameters);
            bitmap.Dispose();
        }
    }
}
