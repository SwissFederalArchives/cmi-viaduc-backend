using System.Collections.Generic;
using CMI.Contract.DocumentConverter;

namespace CMI.Manager.DocumentConverter.Extraction.Interfaces
{
    public interface ITextExtractor
    {
        string Extract(IDoc doc);
        bool HasExtractorFor(string extension);
        IEnumerable<string> GetAllowedAvailableExtensions();
    }
}