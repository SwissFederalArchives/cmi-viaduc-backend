using CMI.Contract.DocumentConverter;

namespace CMI.Manager.DocumentConverter.Extraction.Interfaces
{
    public interface ITextExtractorSettings
    {
        [DefaultValue(int.MaxValue)] int MaxExtractionSize { get; }

        [DefaultValue(null)] string ExplicitAllowedDocExtensions { get; }

        /// <summary>
        /// Gets or sets the text extraction profile that Abbyy should use.
        /// Possible profiles are:  TextExtraction_Accuracy (best quality)
        ///                         TextExtraction_Speed (fastest. About 50% faster as TextExtraction_Accuracy)
        ///                         DocumentArchiving_Accuracy (same quality as TextExtraction_Accuracy but 20% faster)
        ///                         DocumentArchiving_Speed (same speed and quality as TextExtraction_Speed)
        ///                         BookArchiving_Accuracy
        ///                         BookArchiving_Speed
        ///                         DocumentConversion_Accuracy
        ///                         DocumentConversion_Speed
        /// </summary>
        /// <value>The text extraction profile.</value>
        string TextExtractionProfile { get; set; }

        JobContext Context { get; set; }
    }
}
