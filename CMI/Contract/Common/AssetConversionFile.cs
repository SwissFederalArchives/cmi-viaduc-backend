namespace CMI.Contract.Common
{
    /// <summary>
    /// Class used for converting files, with the possibility to split
    /// large (pdf) files in smaller parts.
    /// </summary>
    public class AssetConversionFile: AssetFileBase
    {
        public string ConvertedFile { get; set; }
    }
}