using System;
using System.Runtime.InteropServices;
using Serilog;

namespace CMI.Access.Harvest.ScopeArchiv
{
    /// <summary>
    ///     Helper class for Blob data.
    /// </summary>
    internal class BlobHelper
    {
        private static readonly int MimeSampleSize = 256;

        private static readonly string DefaultMimeType = "application/octet-stream";

        [DllImport("urlmon.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = false)]
        private static extern int FindMimeFromData(IntPtr pBC,
            [MarshalAs(UnmanagedType.LPWStr)] string pwzUrl,
            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.I1, SizeParamIndex = 3)]
            byte[] pBuffer,
            int cbSize,
            [MarshalAs(UnmanagedType.LPWStr)] string pwzMimeProposed,
            int dwMimeFlags,
            out IntPtr ppwzMimeOut,
            int dwReserved);

        /// <summary>
        ///     Gets the MIME types given the bytes
        /// </summary>
        /// <param name="data">The data to analyze.</param>
        /// <returns>The mime type of the data.</returns>
        public static string GetMimeFromBytes(byte[] data)
        {
            try
            {
                FindMimeFromData(IntPtr.Zero, null, data, MimeSampleSize, null, 0, out var mimePointer, 0);

                var mime = Marshal.PtrToStringUni(mimePointer);
                Marshal.FreeCoTaskMem(mimePointer);

                if (mime == "image/pjpeg")
                {
                    mime = "image/jpeg";
                }

                return mime ?? DefaultMimeType;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to ge the mime type. Return the default mime type of {DefaultMimeType}", DefaultMimeType);
                return DefaultMimeType;
            }
        }
    }
}