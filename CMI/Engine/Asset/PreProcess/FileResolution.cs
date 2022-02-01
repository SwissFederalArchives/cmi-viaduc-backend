using System;
using System.Drawing;
using System.IO;
using System.Xml;
using CMI.Engine.Asset.ParameterSettings;

namespace CMI.Engine.Asset.PreProcess
{
    public class FileResolution
    {
        private readonly ScansZusammenfassenSettings settings;

        public FileResolution(ScansZusammenfassenSettings settings)
        {
            this.settings = settings;
        }


        /// <summary>
        ///     Reading the resolution from the image file itself is not very secure. Either the Aspose Library cannot do it
        ///     correctly or
        ///     it might be a problem of the JPEG2000 format.
        ///     For that reason, we read the resolution from the PREMIS file, or the resolution of the image, if no PREMIS file can be
        ///     found. If no Image exists we return the default.    
        /// </summary>
        internal int GetResolution(Image bitmap, string filePath)
        {
            var premisFile = GetPremisFileForImage(filePath);

            if (!File.Exists(premisFile))
            {
                return BitmapOrDefaultResolution(bitmap);
            }

            var fileNameWithoutPath = Path.GetFileName(filePath);
            
            var directoryName = Path.GetDirectoryName(filePath);
            if (string.IsNullOrEmpty(fileNameWithoutPath) || string.IsNullOrEmpty(directoryName))
            {
                return BitmapOrDefaultResolution(bitmap);
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
                    return BitmapOrDefaultResolution(bitmap);
                }

                var resolution = formatNoteNode.InnerText.Replace("resolution=", string.Empty).Replace("dpi", string.Empty);

                return !int.TryParse(resolution, out var result) ? settings.DefaultAufloesungInDpi : result;
            }
            catch (Exception)
            {
                return BitmapOrDefaultResolution(bitmap);
            }
        }

        private int BitmapOrDefaultResolution(Image bitmap)
        {
            if (bitmap != null)
            {
                var bitmapResolution = (int)bitmap.HorizontalResolution;
                // Min Resolution >  150
                if (bitmapResolution <= 150)
                {
                    bitmapResolution = settings.DefaultAufloesungInDpi;
                }

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
    }
}
