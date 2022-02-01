using System.Threading.Tasks;
using CMI.Contract.DocumentConverter;

namespace CMI.Engine.Asset
{
    public interface IRenderEngine
    {
        /// <summary>
        ///     Converts the given file according to the requested destination extension.
        /// </summary>
        /// <param name="file">The full file name.</param>
        /// <param name="destinationExtension">The destination extension.</param>
        /// <param name="context">The context of the conversion</param>
        /// <returns>The name of the converted file</returns>
        Task<string> ConvertFile(string file, string destinationExtension, JobContext context);

        /// <summary>
        ///     Returns a list with the supported file types.
        /// </summary>
        /// <value>The supported file types.</value>
        Task<string[]> GetSupportedFileTypes();
    }
}