using System.Threading.Tasks;
using CMI.Contract.DocumentConverter;

namespace CMI.Engine.Asset
{
    public interface ITextEngine
    {
        Task<string[]> GetSupportedFileTypes();
        Task<string> ExtractText(string file, JobContext context);
    }
}
