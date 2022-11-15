using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace CMI.Web.Common.Helpers
{
    public class ServiceHelper : IServiceHelper
    {

        public static CmiSettings Settings => WebHelper.Settings != null ? WebHelper.Settings : new CmiSettings();

        #region Utilities

        public static int AdjustPagingSkip(int skip, int take, int total)
        {
            if (skip >= total)
            {
                if (take > 0 && total > 0)
                {
                    var i = total - 1;
                    skip = i - i % take;
                }
                else
                {
                    skip = 0;
                }
            }

            return skip;
        }

        public static string AddToString(string s1, string delim, string s2)
        {
            var s = s1;
            if (string.IsNullOrEmpty(s1))
            {
                s = s2;
            }
            else if (!string.IsNullOrEmpty(s2))
            {
                if (string.IsNullOrEmpty(delim))
                {
                    s = s1 + s2;
                }
                else
                {
                    if (s2.StartsWith(delim))
                    {
                        s2 = s2.Remove(0, delim.Length);
                    }

                    s = s1 + (s1.EndsWith(delim) ? string.Empty : delim) + s2;
                }
            }

            return s;
        }

        public static string GetExceptionInfo(Exception exception, int maxDepth = 99)
        {
            var info = string.Empty;
            var depth = 0;
            var ex = exception;
            while (ex != null && depth < maxDepth)
            {
                info += string.Format("[{0:00}] {1}", depth, ex.Message);
                info += Environment.NewLine + Environment.NewLine + ex.StackTrace;
                depth += 1;
                ex = ex.InnerException;
            }

            return info;
        }


        public static object GetFormData<T>(NameValueCollection formData)
        {
            if (formData.HasKeys())
            {
                var unescapedFormData = Uri.UnescapeDataString(formData.GetValues(0).FirstOrDefault() ?? string.Empty);
                if (!string.IsNullOrEmpty(unescapedFormData))
                {
                    return JsonConvert.DeserializeObject<T>(unescapedFormData);
                }
            }

            return null;
        }


        public static MultipartFormDataStreamProvider GetFileMultipartProvider(string uploadPath)
        {
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            return new MultipartFormDataStreamProvider(uploadPath);
        }

        public static InMemoryMultipartFormDataStreamProvider GetInMemoryMultipartProvider()
        {
            return new InMemoryMultipartFormDataStreamProvider();
        }

        #endregion
    }

    public class FileData
    {
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public byte[] Data { get; set; }

        public long Size => Data != null ? Data.LongLength : 0L;

        /// <summary>
        ///     Create a FileData from HttpContent
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static async Task<FileData> ReadFile(HttpContent file)
        {
            var data = await file.ReadAsByteArrayAsync();
            var result = new FileData
            {
                FileName = FixFilename(file.Headers.ContentDisposition.FileName),
                ContentType = file.Headers.ContentType.ToString(),
                Data = data
            };
            return result;
        }

        /// <summary>
        ///     Amend filenames to remove surrounding quotes and remove path from IE
        /// </summary>
        /// <param name="original"></param>
        /// <returns></returns>
        private static string FixFilename(string original)
        {
            var result = original.Trim();
            // remove leading and trailing quotes
            if (result.StartsWith("\""))
            {
                result = result.TrimStart('"').TrimEnd('"');
            }

            // remove full path versions
            if (result.Contains("\\"))
                // parse out path
            {
                result = new FileInfo(result).Name;
            }

            return result;
        }
    }

    public class InMemoryMultipartFormDataStreamProvider : MultipartStreamProvider
    {
        // Set of indexes of which HttpContents we designate as form data
        private readonly Collection<bool> _isFormData = new Collection<bool>();

        /// <summary>
        ///     Gets a <see cref="NameValueCollection" /> of form data passed as part of the multipart form data.
        /// </summary>
        public NameValueCollection FormData { get; } = new NameValueCollection();

        /// <summary>
        ///     Gets list of <see cref="HttpContent" />s which contain uploaded files as in-memory representation.
        /// </summary>
        public List<HttpContent> Files { get; } = new List<HttpContent>();

        /// <summary>
        ///     Convert list of HttpContent items to FileData class task
        /// </summary>
        /// <returns></returns>
        public async Task<FileData[]> GetFiles()
        {
            return await Task.WhenAll(Files.Select(f => FileData.ReadFile(f)));
        }

        public override Stream GetStream(HttpContent parent, HttpContentHeaders headers)
        {
            // For form data, Content-Disposition header is a requirement
            var contentDisposition = headers.ContentDisposition;
            if (contentDisposition != null)
            {
                // We will post process this as form data
                _isFormData.Add(string.IsNullOrEmpty(contentDisposition.FileName));

                return new MemoryStream();
            }

            // If no Content-Disposition header was present.
            throw new InvalidOperationException(string.Format("Did not find required '{0}' header field in MIME multipart body part..",
                "Content-Disposition"));
        }

        /// <summary>
        ///     Read the non-file contents as form data.
        /// </summary>
        /// <returns></returns>
        public override async Task ExecutePostProcessingAsync()
        {
            // Find instances of non-file HttpContents and read them asynchronously
            // to get the string content and then add that as form data
            for (var index = 0; index < Contents.Count; index++)
            {
                if (_isFormData[index])
                {
                    var formContent = Contents[index];
                    // Extract name from Content-Disposition header. We know from earlier that the header is present.
                    var contentDisposition = formContent.Headers.ContentDisposition;
                    var formFieldName = UnquoteToken(contentDisposition.Name) ?? string.Empty;

                    // Read the contents as string data and add to form data
                    var formFieldValue = await formContent.ReadAsStringAsync();
                    FormData.Add(formFieldName, formFieldValue);
                }
                else
                {
                    Files.Add(Contents[index]);
                }
            }
        }

        /// <summary>
        ///     Remove bounding quotes on a token if present
        /// </summary>
        /// <param name="token">Token to unquote.</param>
        /// <returns>Unquoted token.</returns>
        private static string UnquoteToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return token;
            }

            if (token.StartsWith("\"", StringComparison.Ordinal) && token.EndsWith("\"", StringComparison.Ordinal) && token.Length > 1)
            {
                return token.Substring(1, token.Length - 2);
            }

            return token;
        }
    }
}