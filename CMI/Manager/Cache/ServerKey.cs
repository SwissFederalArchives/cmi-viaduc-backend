using System;
using System.Text;
using Rebex.Net;

namespace CMI.Manager.Cache
{
    /// <summary>
    ///     Helper class for getting an SSH private key.
    ///     The PrivateKey is set to a Base64-encoded pregenerated private key.
    ///     Make sure to generate your own key if you intend to use this code in production.
    ///     See http://www.rebex.net/sftp.net/features/private-keys.aspx for more information about SSH keys.
    /// </summary>
    internal static class ServerKey
    {
        /// <summary>
        ///     Loads and returns the pregenerated private key.
        /// </summary>
        /// <returns>An instance of <see cref="SshPrivateKey" /> class.</returns>
        public static SshPrivateKey GetServerPrivateKey()
        {
            var content = Encoding.UTF8.GetString(Convert.FromBase64String(Properties.CacheSettings.Default.SftpPrivateCertKey));
            var data = Encoding.ASCII.GetBytes(content);
            return new SshPrivateKey(data, Properties.CacheSettings.Default.SftpPrivateCertPassword);
        }
    }
}