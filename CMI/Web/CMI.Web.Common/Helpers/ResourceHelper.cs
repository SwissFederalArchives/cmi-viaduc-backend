using System.IO;
using System.Reflection;
using System.Text;

namespace CMI.Web.Common.Helpers
{
    public static class ResourceHelper
    {
        public static string ReadResource(string resourceName)
        {
            var assembly = Assembly.GetCallingAssembly();
            using (var textStream = assembly.GetManifestResourceStream(resourceName))
            {
                if (textStream == null)
                {
                    return null;
                }

                using (var sr = new StreamReader(textStream, Encoding.Default, false))
                {
                    return sr.ReadToEnd();
                }
            }
        }
    }
}