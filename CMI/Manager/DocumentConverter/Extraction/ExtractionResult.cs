using System.Collections.Generic;
using System.IO;
using System.Text;
using CMI.Manager.DocumentConverter.Extraction.Interfaces;
using OcrResultType = CMI.Contract.DocumentConverter.OcrResultType;

namespace CMI.Manager.DocumentConverter.Extraction
{
    public class ExtractionResult
    {
        private readonly int maxSize;
        private readonly StringBuilder sb = new StringBuilder();

        public ExtractionResult(int maxResultSize)
        {
            maxSize = maxResultSize;
        }

        public int CharactersLeft => maxSize - sb.Length;

        public bool LimitExceeded => sb.Length > maxSize;

        public bool HasError { get; set; }

        public string ErrorMessage { get; set; }

        /// <summary>
        /// Indicates if the result was created from an OCR process
        /// </summary>
        public bool IsOcrResult { get; set; }

        /// <summary>
        /// A list with created documents during an OCR process
        /// </summary>
        public Dictionary<OcrResultType, string> CreatedOcrFiles { get; set; } = new();

        public bool HasContent
        {
            get
            {
                if (sb.Length == 0)
                {
                    return false;
                }

                return !string.IsNullOrWhiteSpace(sb.ToString());
            }
        }

        public void Append(string text)
        {
            if (text == null)
            {
                return;
            }

            var s = text.Trim();

            if (s.Length > 1)
            {
                sb.AppendLine(s);
            }
        }

        public override string ToString()
        {
            return sb.ToString();
        }
    }
}