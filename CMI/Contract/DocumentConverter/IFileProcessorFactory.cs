using System.Collections.Generic;

namespace CMI.Contract.DocumentConverter
{
    public interface IFileProcessorFactory
    {
        IEnumerable<string> GetAvailableExtensions();

        bool IsValidExtension(string extension);
    }
}