namespace CMI.Contract.Common
{
    /// <summary>
    /// Class used for extracting text from files, with the possibility to split
    /// large (pdf) files in smaller parts.
    /// </summary>
    public class AssetExtractionFile: AssetFileBase
    {
        /// <summary>
        /// Contains the extracted text
        /// </summary>
        public string ContentText { get; set; }
    }
}
