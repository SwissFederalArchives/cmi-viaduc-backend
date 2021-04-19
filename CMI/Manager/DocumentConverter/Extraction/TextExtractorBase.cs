using System;
using System.Collections.Generic;
using System.Linq;
using CMI.Contract.DocumentConverter;
using CMI.Manager.DocumentConverter.Extraction.Interfaces;

namespace CMI.Manager.DocumentConverter.Extraction
{
    public abstract class TextExtractorBase
    {
        public abstract IEnumerable<string> AllowedExtensions { get; }

        public abstract int Rank { get; }

        public abstract bool IsAvailable { get; }

        public abstract ExtractionResult ExtractText(IDoc doc, ITextExtractorSettings settings);

        public bool SupportsExtension(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
            {
                return false;
            }

            var ext = extension.StartsWith(".") ? extension.Substring(1) : extension;

            return AllowedExtensions.Any(e => e.Equals(ext, StringComparison.InvariantCultureIgnoreCase));
        }
    }
}