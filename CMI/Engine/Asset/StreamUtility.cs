using System.IO;

namespace CMI.Engine.Asset
{
    public static class StreamUtility
    {
        public static string MemoryStreamToString(MemoryStream ms)
        {
            ms.Position = 0;
            // Create a stream reader.
            using (var reader = new StreamReader(ms))
            {
                // Just read to the end.
                return reader.ReadToEnd();
            }
        }
    }
}