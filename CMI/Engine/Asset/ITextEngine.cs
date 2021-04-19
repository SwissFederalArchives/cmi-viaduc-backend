using System.Threading.Tasks;

namespace CMI.Engine.Asset
{
    public interface ITextEngine
    {
        Task<string[]> GetSupportedFileTypes();
        Task<string> ExtractText(string file);
    }
}