using CMI.Contract.DocumentConverter;
using CMI.Manager.DocumentConverter.Extraction.Interfaces;

namespace CMI.Manager.DocumentConverter.Extraction
{
    public class DefaultTextExtractorSettings : ITextExtractorSettings
    {
        public DefaultTextExtractorSettings(string textExtractionProfile)
        {
            MaxExtractionSize = int.MaxValue;
            ExplicitAllowedDocExtensions = null;
            TextExtractionProfile = textExtractionProfile;
        }

        public int MaxExtractionSize { get; }
        public string ExplicitAllowedDocExtensions { get; }
        public string TextExtractionProfile { get; set; }
        public JobContext Context { get; set; }
    }
}
