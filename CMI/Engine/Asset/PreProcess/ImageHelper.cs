using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using CMI.Engine.Asset.ParameterSettings;
using Serilog;
using Image = System.Drawing.Image;


namespace CMI.Engine.Asset.PreProcess
{
    public class ImageHelper
    {
        private readonly ScansZusammenfassenSettings settings;

        public ImageHelper(ScansZusammenfassenSettings settings)
        {
            this.settings = settings;
        }

        /// <summary>
        ///     Reading the resolution from the image file itself is not very secure. Either the Aspose Library cannot do it
        ///     correctly or it might be a problem of the JPEG2000 format.
        ///     For that reason, we read the resolution from the PREMIS file, or the resolution of the image, if no PREMIS file can be
        ///     found. If no Image exists we return the default.    
        /// </summary>
        public int GetResolution(string filePath)
        {
            var premisFile = GetPremisFileForImage(filePath);

            if (!File.Exists(premisFile))
            {
                return BitmapOrDefaultResolution(filePath);
            }

            var fileNameWithoutPath = Path.GetFileName(filePath);

            var directoryName = Path.GetDirectoryName(filePath);
            if (string.IsNullOrEmpty(fileNameWithoutPath) || string.IsNullOrEmpty(directoryName))
            {
                return BitmapOrDefaultResolution(filePath);
            }

            try
            {
                var doc = new XmlDocument();
                doc.Load(premisFile);
                XmlNamespaceManager xmlNamespaceManager = new XmlNamespaceManager(doc.NameTable);
                xmlNamespaceManager.AddNamespace("premis", "http://www.loc.gov/premis/v3");

                XmlElement root = doc.DocumentElement;
                var objectNode = root?.SelectSingleNode($"//premis:object[premis:originalName/text()='{fileNameWithoutPath}']", xmlNamespaceManager);
                var formatNoteNode = objectNode?.SelectSingleNode("//premis:formatNote[starts-with(.,'resolution=')]", xmlNamespaceManager);

                if (formatNoteNode == null)
                {
                    return BitmapOrDefaultResolution(filePath);
                }

                var resolution = formatNoteNode.InnerText.Replace("resolution=", string.Empty).Replace("dpi", string.Empty);

                return !int.TryParse(resolution, out var result) ? settings.DefaultAufloesungInDpi : result;
            }
            catch (Exception)
            {
                return BitmapOrDefaultResolution(filePath);
            }
        }

        public Size GetImageSize(string filePath)
        {
            var premisFile = GetPremisFileForImage(filePath);

            if (!File.Exists(premisFile))
            {
                return GetBitmapSize(filePath);
            }

            var fileNameWithoutPath = Path.GetFileName(filePath);

            var directoryName = Path.GetDirectoryName(filePath);
            if (string.IsNullOrEmpty(fileNameWithoutPath) || string.IsNullOrEmpty(directoryName))
            {
                return GetBitmapSize(filePath);
            }

            try
            {
                Log.Information("Trying to read image size from premis file for file {premisFile}", premisFile);
                var doc = new XmlDocument();
                doc.Load(premisFile);
                XmlNamespaceManager xmlNamespaceManager = new XmlNamespaceManager(doc.NameTable);
                xmlNamespaceManager.AddNamespace("premis", "http://www.loc.gov/premis/v3");

                XmlElement root = doc.DocumentElement;
                var objectNode = root?.SelectSingleNode($"//premis:object[premis:originalName/text()='{fileNameWithoutPath}']", xmlNamespaceManager);
                var widthNode = objectNode?.SelectSingleNode("//premis:formatNote[starts-with(.,'ImageWidth=')]", xmlNamespaceManager);
                // Vecteur hat Schreibfehler. Daher testen wir auf beide Varianten, falls sie es mal korrigieren
                var heightNode = objectNode?.SelectSingleNode("//premis:formatNote[starts-with(.,'ImageHeight=')]", xmlNamespaceManager) ?? objectNode?.SelectSingleNode("//premis:formatNote[starts-with(.,'ImageHight=')]", xmlNamespaceManager);

                if (widthNode == null || heightNode == null)
                {
                    return GetBitmapSize(filePath);
                }

                Log.Information("Found image size information for file {premisFile}", premisFile);

                var width = widthNode.InnerText.Replace("ImageWidth=", string.Empty).Replace("pixel", string.Empty);
                var height = heightNode.InnerText.Replace("ImageHeight=", string.Empty).Replace("ImageHight=", string.Empty).Replace("pixel", string.Empty);

                if (int.TryParse(width, out var widthResult) && int.TryParse(height, out var heighResult))
                {
                    return new Size(widthResult, heighResult);
                }

                return GetBitmapSize(filePath);
            }
            catch (Exception)
            {
                return GetBitmapSize(filePath);
            }
        }

        public string ConvertToJpeg(string inputFile, int groesseInProzent, int qualitaetInProzent)
        {
            return Convert(inputFile, ".jpg", groesseInProzent, qualitaetInProzent);
        }

        public string ConvertToPdf(string inputFile, int groesseInProzent, int qualitaetInProzent)
        {
            // First convert to jpg as some huge TIFFs don't convert correctly to pdf
            var jpgFile = Convert(inputFile, ".jpg", groesseInProzent, qualitaetInProzent);

            // As we already reduced size and quality in the jpeg, we won't reduce it again
            var outputFile = Convert(jpgFile, ".pdf", 100, 100);

            // Delete temp jpg file
            File.Delete(jpgFile);

            return outputFile;
        }

        public int GetResolutionFromBitmap(Image bitmap)
        {
            // If the resolution of the bitmap is less than 150, it's not correct
            // as our images are normally 300dpi.
            // After converting JP2 to jpg usually the dpi is reported as 72dpi
            // Tiff images report the dpi correctly
            if (bitmap.HorizontalResolution <= 150)
                return settings.DefaultAufloesungInDpi;

            return (int) bitmap.HorizontalResolution;
        }

        private Size GetBitmapSize(string filePath)
        {
            var retVal = new Size();

            using (var process = new Process())
            {
                process.StartInfo.FileName = "magick.exe";
                process.StartInfo.Arguments = $"identify \"{filePath}\"";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.Start();

                // Synchronously read the standard output of the spawned process.
                var reader = process.StandardOutput;
                string output = reader.ReadToEnd();

                var match = Regex.Match(output.Replace(filePath, string.Empty), @"\s(?<width>\d*)x(?<height>\d*)\s");
                if (match.Success)
                {
                    if (int.TryParse(match.Groups["width"].Value, out var width))
                    {
                        retVal.Width = width;
                    }

                    if (int.TryParse(match.Groups["height"].Value, out var height))
                    {
                        retVal.Height = height;
                    }
                }

                process.WaitForExit();
            }
            return retVal;
        }

        private int GetBitmapResolution(string filePath)
        {
            var retVal = 0;

            try
            {
                using (var process = new Process())
                {
                    process.StartInfo.FileName = "magick.exe";
                    process.StartInfo.Arguments = $"identify -format \"%x\" \"{filePath}\"";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.Start();

                    // Synchronously read the standard output of the spawned process.
                    var reader = process.StandardOutput;
                    string output = reader.ReadToEnd();

                    if (float.TryParse(output, out var resolution))
                    {
                        retVal = (int) resolution;
                    }

                    process.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, $"Unable to identify image {filePath}");
            }

            return retVal;
        }

        private int BitmapOrDefaultResolution(string path)
        {
            var bitmapResolution = GetBitmapResolution(path);

            // Min Resolution >  150
            if (bitmapResolution > 150)
            {
                return bitmapResolution;
            }

            return settings.DefaultAufloesungInDpi;
        }

        private string GetPremisFileForImage(string imageFileName)
        {
            // The premis file has the same name as the image file, but with _PREMIS as a suffix
            // Rule valid since April 2019
            // Before the image name was [000000001]_DIG.jp2
            return imageFileName.Remove(imageFileName.Length - Path.GetExtension(imageFileName).Length) + "_PREMIS.xml";
        }

        private static string Convert(string inputFile, string destinationExtension, int groesseInProzent, int qualitaetInProzent)
        {
            var outputFile = Path.ChangeExtension(inputFile, destinationExtension);

            using (var process = new Process())
            {
                process.StartInfo.FileName = "magick.exe";
                process.StartInfo.Arguments =
                    $"convert \"{inputFile}\" -quality {qualitaetInProzent} {(groesseInProzent != 100 && groesseInProzent > 0 ? $"-resize {groesseInProzent}%" : string.Empty)} \"{outputFile}\"";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.Start();

                // Synchronously read the standard output of the spawned process.
                var reader = process.StandardOutput;
                string output = reader.ReadToEnd();

                // Write the redirected output to this application's window.
                Log.Information("Converted {inputFile} to {outputFile}: Result is {output}", inputFile, outputFile,
                    string.IsNullOrEmpty(output) ? "ok" : output);

                process.WaitForExit();
            }

            return outputFile;
        }
    }
}
