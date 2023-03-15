using System.Collections.Generic;
using System.IO;

namespace CMI.Contract.DocumentConverter
{
    public class ExtractionStartResult : JobInitResult
    {
        public string Text { get; set; }

        /// <summary>
        /// Indicates if the result was created from an OCR process
        /// </summary>
        public bool IsOcrResult { get; set; }

        /// <summary>
        /// A list with created documents during an OCR process
        /// </summary>
        public Dictionary<OcrResultType, string> CreatedOcrFiles { get; set; } = new();
    }
}